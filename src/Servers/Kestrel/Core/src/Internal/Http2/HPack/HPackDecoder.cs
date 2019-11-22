// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal class HPackDecoder
    {
        private enum State
        {
            Ready,
            HeaderFieldIndex,
            HeaderNameIndex,
            HeaderNameLength,
            HeaderNameLengthContinue,
            HeaderName,
            HeaderValueLength,
            HeaderValueLengthContinue,
            HeaderValue,
            DynamicTableSizeUpdate
        }

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.1
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 1 |        Index (7+)         |
        // +---+---------------------------+
        private const byte IndexedHeaderFieldMask = 0x80;
        private const byte IndexedHeaderFieldRepresentation = 0x80;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.1
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 1 |      Index (6+)       |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldWithIncrementalIndexingMask = 0xc0;
        private const byte LiteralHeaderFieldWithIncrementalIndexingRepresentation = 0x40;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.2
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 0 | 0 |  Index (4+)   |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldWithoutIndexingMask = 0xf0;
        private const byte LiteralHeaderFieldWithoutIndexingRepresentation = 0x00;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.3
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 0 | 1 |  Index (4+)   |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldNeverIndexedMask = 0xf0;
        private const byte LiteralHeaderFieldNeverIndexedRepresentation = 0x10;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.3
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 1 |   Max size (5+)   |
        // +---+---------------------------+
        private const byte DynamicTableSizeUpdateMask = 0xe0;
        private const byte DynamicTableSizeUpdateRepresentation = 0x20;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | H |    String Length (7+)     |
        // +---+---------------------------+
        private const byte HuffmanMask = 0x80;

        private const int IndexedHeaderFieldPrefix = 7;
        private const int LiteralHeaderFieldWithIncrementalIndexingPrefix = 6;
        private const int LiteralHeaderFieldWithoutIndexingPrefix = 4;
        private const int LiteralHeaderFieldNeverIndexedPrefix = 4;
        private const int DynamicTableSizeUpdatePrefix = 5;
        private const int StringLengthPrefix = 7;

        private readonly int _maxDynamicTableSize;
        private readonly DynamicTable _dynamicTable;
        private readonly IntegerDecoder _integerDecoder = new IntegerDecoder();
        private readonly byte[] _stringOctets;
        private readonly byte[] _headerNameOctets;
        private readonly byte[] _headerValueOctets;

        private State _state = State.Ready;
        private byte[] _headerName;
        private int _stringIndex;
        private int _stringLength;
        private int _headerNameLength;
        private int _headerValueLength;
        private bool _index;
        private bool _huffman;
        private bool _headersObserved;

        public HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize)
            : this(maxDynamicTableSize, maxRequestHeaderFieldSize, new DynamicTable(maxDynamicTableSize)) { }

        // For testing.
        internal HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize, DynamicTable dynamicTable)
        {
            _maxDynamicTableSize = maxDynamicTableSize;
            _dynamicTable = dynamicTable;

            _stringOctets = new byte[maxRequestHeaderFieldSize];
            _headerNameOctets = new byte[maxRequestHeaderFieldSize];
            _headerValueOctets = new byte[maxRequestHeaderFieldSize];
        }

        public void Decode(in ReadOnlySequence<byte> data, bool endHeaders, IHttpHeadersHandler handler)
        {
            foreach (var segment in data)
            {
                var span = segment.Span;
                for (var i = 0; i < span.Length; i++)
                {
                    OnByte(span[i], handler);
                }
            }

            if (endHeaders)
            {
                if (_state != State.Ready)
                {
                    throw new HPackDecodingException(CoreStrings.HPackErrorIncompleteHeaderBlock);
                }

                _headersObserved = false;
            }
        }

        private void OnByte(byte b, IHttpHeadersHandler handler)
        {
            int intResult;
            switch (_state)
            {
                case State.Ready:
                    if ((b & IndexedHeaderFieldMask) == IndexedHeaderFieldRepresentation)
                    {
                        _headersObserved = true;
                        var val = b & ~IndexedHeaderFieldMask;

                        if (_integerDecoder.BeginTryDecode((byte)val, IndexedHeaderFieldPrefix, out intResult))
                        {
                            OnIndexedHeaderField(intResult, handler);
                        }
                        else
                        {
                            _state = State.HeaderFieldIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldWithIncrementalIndexingMask) == LiteralHeaderFieldWithIncrementalIndexingRepresentation)
                    {
                        _headersObserved = true;
                        _index = true;
                        var val = b & ~LiteralHeaderFieldWithIncrementalIndexingMask;

                        if (val == 0)
                        {
                            _state = State.HeaderNameLength;
                        }
                        else if (_integerDecoder.BeginTryDecode((byte)val, LiteralHeaderFieldWithIncrementalIndexingPrefix, out intResult))
                        {
                            OnIndexedHeaderName(intResult);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldWithoutIndexingMask) == LiteralHeaderFieldWithoutIndexingRepresentation)
                    {
                        _headersObserved = true;
                        _index = false;
                        var val = b & ~LiteralHeaderFieldWithoutIndexingMask;

                        if (val == 0)
                        {
                            _state = State.HeaderNameLength;
                        }
                        else if (_integerDecoder.BeginTryDecode((byte)val, LiteralHeaderFieldWithoutIndexingPrefix, out intResult))
                        {
                            OnIndexedHeaderName(intResult);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldNeverIndexedMask) == LiteralHeaderFieldNeverIndexedRepresentation)
                    {
                        _headersObserved = true;
                        _index = false;
                        var val = b & ~LiteralHeaderFieldNeverIndexedMask;

                        if (val == 0)
                        {
                            _state = State.HeaderNameLength;
                        }
                        else if (_integerDecoder.BeginTryDecode((byte)val, LiteralHeaderFieldNeverIndexedPrefix, out intResult))
                        {
                            OnIndexedHeaderName(intResult);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & DynamicTableSizeUpdateMask) == DynamicTableSizeUpdateRepresentation)
                    {
                        // https://tools.ietf.org/html/rfc7541#section-4.2
                        // This dynamic table size
                        // update MUST occur at the beginning of the first header block
                        // following the change to the dynamic table size.
                        if (_headersObserved)
                        {
                            throw new HPackDecodingException(CoreStrings.HPackErrorDynamicTableSizeUpdateNotAtBeginningOfHeaderBlock);
                        }

                        if (_integerDecoder.BeginTryDecode((byte)(b & ~DynamicTableSizeUpdateMask), DynamicTableSizeUpdatePrefix, out intResult))
                        {
                            SetDynamicHeaderTableSize(intResult);
                        }
                        else
                        {
                            _state = State.DynamicTableSizeUpdate;
                        }
                    }
                    else
                    {
                        // Can't happen
                        throw new HPackDecodingException($"Byte value {b} does not encode a valid header field representation.");
                    }

                    break;
                case State.HeaderFieldIndex:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnIndexedHeaderField(intResult, handler);
                    }

                    break;
                case State.HeaderNameIndex:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnIndexedHeaderName(intResult);
                    }

                    break;
                case State.HeaderNameLength:
                    _huffman = (b & HuffmanMask) != 0;

                    if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.HeaderName);
                    }
                    else
                    {
                        _state = State.HeaderNameLengthContinue;
                    }

                    break;
                case State.HeaderNameLengthContinue:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.HeaderName);
                    }

                    break;
                case State.HeaderName:
                    _stringOctets[_stringIndex++] = b;

                    if (_stringIndex == _stringLength)
                    {
                        OnString(nextState: State.HeaderValueLength);
                    }

                    break;
                case State.HeaderValueLength:
                    _huffman = (b & HuffmanMask) != 0;

                    if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.HeaderValue);
                        if (intResult == 0)
                        {
                            ProcessHeaderValue(handler);
                        }
                    }
                    else
                    {
                        _state = State.HeaderValueLengthContinue;
                    }

                    break;
                case State.HeaderValueLengthContinue:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnStringLength(intResult, nextState: State.HeaderValue);
                        if (intResult == 0)
                        {
                            ProcessHeaderValue(handler);
                        }
                    }

                    break;
                case State.HeaderValue:
                    _stringOctets[_stringIndex++] = b;

                    if (_stringIndex == _stringLength)
                    {
                        ProcessHeaderValue(handler);
                    }

                    break;
                case State.DynamicTableSizeUpdate:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        SetDynamicHeaderTableSize(intResult);
                        _state = State.Ready;
                    }

                    break;
                default:
                    // Can't happen
                    throw new HPackDecodingException("The HPACK decoder reached an invalid state.");
            }
        }

        private void ProcessHeaderValue(IHttpHeadersHandler handler)
        {
            OnString(nextState: State.Ready);

            var headerNameSpan = new Span<byte>(_headerName, 0, _headerNameLength);
            var headerValueSpan = new Span<byte>(_headerValueOctets, 0, _headerValueLength);

            handler.OnHeader(headerNameSpan, headerValueSpan);

            if (_index)
            {
                _dynamicTable.Insert(headerNameSpan, headerValueSpan);
            }
        }

        private void OnIndexedHeaderField(int index, IHttpHeadersHandler handler)
        {
            var header = GetHeader(index);
            handler.OnHeader(new Span<byte>(header.Name), new Span<byte>(header.Value));
            _state = State.Ready;
        }

        private void OnIndexedHeaderName(int index)
        {
            var header = GetHeader(index);
            _headerName = header.Name;
            _headerNameLength = header.Name.Length;
            _state = State.HeaderValueLength;
        }

        private void OnStringLength(int length, State nextState)
        {
            if (length > _stringOctets.Length)
            {
                throw new HPackDecodingException(CoreStrings.FormatHPackStringLengthTooLarge(length, _stringOctets.Length));
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void OnString(State nextState)
        {
            int Decode(byte[] dst)
            {
                if (_huffman)
                {
                    return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), dst);
                }
                else
                {
                    Buffer.BlockCopy(_stringOctets, 0, dst, 0, _stringLength);
                    return _stringLength;
                }
            }

            try
            {
                if (_state == State.HeaderName)
                {
                    _headerName = _headerNameOctets;
                    _headerNameLength = Decode(_headerNameOctets);
                }
                else
                {
                    _headerValueLength = Decode(_headerValueOctets);
                }
            }
            catch (HuffmanDecodingException ex)
            {
                throw new HPackDecodingException(CoreStrings.HPackHuffmanError, ex);
            }

            _state = nextState;
        }

        private HeaderField GetHeader(int index)
        {
            try
            {
                return index <= StaticTable.Instance.Count
                    ? StaticTable.Instance[index - 1]
                    : _dynamicTable[index - StaticTable.Instance.Count - 1];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new HPackDecodingException(CoreStrings.FormatHPackErrorIndexOutOfRange(index), ex);
            }
        }

        private void SetDynamicHeaderTableSize(int size)
        {
            if (size > _maxDynamicTableSize)
            {
                throw new HPackDecodingException(
                    CoreStrings.FormatHPackErrorDynamicTableSizeUpdateTooLarge(size, _maxDynamicTableSize));
            }

            _dynamicTable.Resize(size);
        }
    }
}
