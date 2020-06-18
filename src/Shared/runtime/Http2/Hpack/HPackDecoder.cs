// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Diagnostics;
#if KESTREL
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
#endif

namespace System.Net.Http.HPack
{
    internal class HPackDecoder
    {
        private enum State : byte
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

        public const int DefaultHeaderTableSize = 4096;
        public const int DefaultStringOctetsSize = 4096;
        public const int DefaultMaxHeadersLength = 64 * 1024;

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
        private readonly int _maxHeadersLength;
        private readonly DynamicTable _dynamicTable;
        private readonly IntegerDecoder _integerDecoder = new IntegerDecoder();
        private byte[] _stringOctets;
        private byte[] _headerNameOctets;
        private byte[] _headerValueOctets;

        private State _state = State.Ready;
        private byte[]? _headerName;
        private int _stringIndex;
        private int _stringLength;
        private int _headerNameLength;
        private int _headerValueLength;
        private bool _index;
        private bool _huffman;
        private bool _headersObserved;

        public HPackDecoder(int maxDynamicTableSize = DefaultHeaderTableSize, int maxHeadersLength = DefaultMaxHeadersLength)
            : this(maxDynamicTableSize, maxHeadersLength, new DynamicTable(maxDynamicTableSize))
        {
        }

        // For testing.
        internal HPackDecoder(int maxDynamicTableSize, int maxHeadersLength, DynamicTable dynamicTable)
        {
            _maxDynamicTableSize = maxDynamicTableSize;
            _maxHeadersLength = maxHeadersLength;
            _dynamicTable = dynamicTable;

            _stringOctets = new byte[DefaultStringOctetsSize];
            _headerNameOctets = new byte[DefaultStringOctetsSize];
            _headerValueOctets = new byte[DefaultStringOctetsSize];
        }

        public void Decode(in ReadOnlySequence<byte> data, bool endHeaders, IHttpHeadersHandler handler)
        {
            foreach (ReadOnlyMemory<byte> segment in data)
            {
                DecodeInternal(segment.Span, handler);
            }

            CheckIncompleteHeaderBlock(endHeaders);
        }

        public void Decode(ReadOnlySpan<byte> data, bool endHeaders, IHttpHeadersHandler? handler)
        {
            DecodeInternal(data, handler);
            CheckIncompleteHeaderBlock(endHeaders);
        }

        private void DecodeInternal(ReadOnlySpan<byte> data, IHttpHeadersHandler? handler)
        {
            int currentIndex = 0;

            do
            {
                switch (_state)
                {
                    case State.Ready:
                        Parse(data, ref currentIndex, handler);
                        break;
                    case State.HeaderFieldIndex:
                        ParseHeaderFieldIndex(data, ref currentIndex, handler);
                        break;
                    case State.HeaderNameIndex:
                        ParseHeaderNameIndex(data, ref currentIndex, handler);
                        break;
                    case State.HeaderNameLength:
                        ParseHeaderNameLength(data, ref currentIndex, handler);
                        break;
                    case State.HeaderNameLengthContinue:
                        ParseHeaderNameLengthContinue(data, ref currentIndex, handler);
                        break;
                    case State.HeaderName:
                        ParseHeaderName(data, ref currentIndex, handler);
                        break;
                    case State.HeaderValueLength:
                        ParseHeaderValueLength(data, ref currentIndex, handler);
                        break;
                    case State.HeaderValueLengthContinue:
                        ParseHeaderValueLengthContinue(data, ref currentIndex, handler);
                        break;
                    case State.HeaderValue:
                        ParseHeaderValue(data, ref currentIndex, handler);
                        break;
                    case State.DynamicTableSizeUpdate:
                        ParseDynamicTableSizeUpdate(data, ref currentIndex);
                        break;
                    default:
                        // Can't happen
                        Debug.Fail("HPACK decoder reach an invalid state");
                        throw new NotImplementedException(_state.ToString());
                }
            }
            while (currentIndex < data.Length);
        }

        private void ParseDynamicTableSizeUpdate(ReadOnlySpan<byte> data, ref int currentIndex)
        {
            for (; currentIndex < data.Length; currentIndex++)
            {
                if (_integerDecoder.TryDecode(data[currentIndex], out var intResult))
                {
                    SetDynamicHeaderTableSize(intResult);
                    _state = State.Ready;
                    currentIndex++;
                    break;
                }
            }
        }

        private void ParseHeaderValueLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            if (currentIndex < data.Length)
            {
                var b = data[currentIndex++];

                _huffman = (b & HuffmanMask) != 0;

                if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out var intResult))
                {
                    OnStringLength(intResult, nextState: State.HeaderValue);

                    if (intResult == 0)
                    {
                        ProcessHeaderValue(handler);
                    }
                    else
                    {
                        ParseHeaderValue(data, ref currentIndex, handler);
                    }
                }
                else
                {
                    _state = State.HeaderValueLengthContinue;
                    ParseHeaderValueLengthContinue(data, ref currentIndex, handler);
                }
            }
        }

        private void ParseHeaderNameLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            for (; currentIndex < data.Length; currentIndex++)
            {
                if (_integerDecoder.TryDecode(data[currentIndex], out var intResult))
                {
                    // IntegerDecoder disallows overlong encodings, where an integer is encoded with more bytes than is strictly required.
                    // 0 should always be represented by a single byte, so we shouldn't need to check for it in the continuation case.
                    Debug.Assert(intResult != 0, "A header name length of 0 should never be encoded with a continuation byte.");

                    OnStringLength(intResult, nextState: State.HeaderName);
                    currentIndex++;
                    ParseHeaderName(data, ref currentIndex, handler);
                    break;
                }
            }
        }

        private void ParseHeaderValueLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            for (; currentIndex < data.Length; currentIndex++)
            {
                if (_integerDecoder.TryDecode(data[currentIndex], out var intResult))
                {
                    // IntegerDecoder disallows overlong encodings where an integer is encoded with more bytes than is strictly required.
                    // 0 should always be represented by a single byte, so we shouldn't need to check for it in the continuation case.
                    Debug.Assert(intResult != 0, "A header value length of 0 should never be encoded with a continuation byte.");

                    OnStringLength(intResult, nextState: State.HeaderValue);
                    currentIndex++;
                    ParseHeaderValue(data, ref currentIndex, handler);
                    break;
                }
            }
        }

        private void ParseHeaderFieldIndex(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            for (; currentIndex < data.Length; currentIndex++)
            {
                if (_integerDecoder.TryDecode(data[currentIndex], out var intResult))
                {
                    OnIndexedHeaderField(intResult, handler);
                    currentIndex++;
                    break;
                }
            }
        }

        private void ParseHeaderNameIndex(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            for (; currentIndex < data.Length; currentIndex++)
            {
                if (_integerDecoder.TryDecode(data[currentIndex], out var intResult))
                {
                    OnIndexedHeaderName(intResult);
                    currentIndex++;
                    ParseHeaderValueLength(data, ref currentIndex, handler);
                    break;
                }
            }
        }

        private void ParseHeaderNameLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            if (currentIndex < data.Length)
            {
                var b = data[currentIndex++];

                _huffman = (b & HuffmanMask) != 0;

                if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out var intResult))
                {
                    if (intResult == 0)
                    {
                        throw new HPackDecodingException(SR.Format(SR.net_http_invalid_header_name, ""));
                    }

                    OnStringLength(intResult, nextState: State.HeaderName);
                    ParseHeaderName(data, ref currentIndex, handler);
                }
                else
                {
                    _state = State.HeaderNameLengthContinue;
                    ParseHeaderNameLengthContinue(data, ref currentIndex, handler);
                }
            }
        }

        private void Parse(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            if (currentIndex < data.Length)
            {
                Debug.Assert(_state == State.Ready, "Should be ready to parse a new header.");

                var b = data[currentIndex++];

                // TODO: Instead of masking and comparing each prefix value,
                // consider doing a 16-way switch on the first four bits (which is the max prefix size).
                // Look at this once we have more concrete perf data.
                if ((b & IndexedHeaderFieldMask) == IndexedHeaderFieldRepresentation)
                {
                    _headersObserved = true;

                    int val = b & ~IndexedHeaderFieldMask;

                    if (_integerDecoder.BeginTryDecode((byte)val, IndexedHeaderFieldPrefix, out var intResult))
                    {
                        OnIndexedHeaderField(intResult, handler);
                    }
                    else
                    {
                        _state = State.HeaderFieldIndex;
                        ParseHeaderFieldIndex(data, ref currentIndex, handler);
                    }
                }
                else if ((b & LiteralHeaderFieldWithIncrementalIndexingMask) == LiteralHeaderFieldWithIncrementalIndexingRepresentation)
                {
                    ParseLiteralHeaderField(
                        data,
                        ref currentIndex,
                        b,
                        LiteralHeaderFieldWithIncrementalIndexingMask,
                        LiteralHeaderFieldWithIncrementalIndexingPrefix,
                        index: true,
                        handler);
                }
                else if ((b & LiteralHeaderFieldWithoutIndexingMask) == LiteralHeaderFieldWithoutIndexingRepresentation)
                {
                    ParseLiteralHeaderField(
                        data,
                        ref currentIndex,
                        b,
                        LiteralHeaderFieldWithoutIndexingMask,
                        LiteralHeaderFieldWithoutIndexingPrefix,
                        index: false,
                        handler);
                }
                else if ((b & LiteralHeaderFieldNeverIndexedMask) == LiteralHeaderFieldNeverIndexedRepresentation)
                {
                    ParseLiteralHeaderField(
                        data,
                        ref currentIndex,
                        b,
                        LiteralHeaderFieldNeverIndexedMask,
                        LiteralHeaderFieldNeverIndexedPrefix,
                        index: false,
                        handler);
                }
                else if ((b & DynamicTableSizeUpdateMask) == DynamicTableSizeUpdateRepresentation)
                {
                    // https://tools.ietf.org/html/rfc7541#section-4.2
                    // This dynamic table size
                    // update MUST occur at the beginning of the first header block
                    // following the change to the dynamic table size.
                    if (_headersObserved)
                    {
                        throw new HPackDecodingException(SR.net_http_hpack_late_dynamic_table_size_update);
                    }

                    if (_integerDecoder.BeginTryDecode((byte)(b & ~DynamicTableSizeUpdateMask), DynamicTableSizeUpdatePrefix, out var intResult))
                    {
                        SetDynamicHeaderTableSize(intResult);
                    }
                    else
                    {
                        _state = State.DynamicTableSizeUpdate;
                        ParseDynamicTableSizeUpdate(data, ref currentIndex);
                    }
                }
                else
                {
                    // Can't happen
                    Debug.Fail("Unreachable code");
                    throw new InvalidOperationException("Unreachable code.");
                }
            }
        }

        private void ParseLiteralHeaderField(ReadOnlySpan<byte> data, ref int currentIndex, byte b, byte mask, byte indexPrefix, bool index, IHttpHeadersHandler? handler)
        {
            _headersObserved = true;

            _index = index;
            int val = b & ~mask;

            if (val == 0)
            {
                _state = State.HeaderNameLength;
                ParseHeaderNameLength(data, ref currentIndex, handler);
            }
            else
            {
                if (_integerDecoder.BeginTryDecode((byte)val, indexPrefix, out var intResult))
                {
                    OnIndexedHeaderName(intResult);
                    ParseHeaderValueLength(data, ref currentIndex, handler);
                }
                else
                {
                    _state = State.HeaderNameIndex;
                    ParseHeaderNameIndex(data, ref currentIndex, handler);
                }
            }
        }

        private void ParseHeaderName(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            // Read remaining chars, up to the length of the current data
            var count = Math.Min(_stringLength - _stringIndex, data.Length - currentIndex);

            data.Slice(currentIndex, count).CopyTo(_stringOctets.AsSpan(_stringIndex));
            _stringIndex += count;
            currentIndex += count;

            if (_stringIndex == _stringLength)
            {
                OnString(nextState: State.HeaderValueLength);
                ParseHeaderValueLength(data, ref currentIndex, handler);
            }
        }

        private void ParseHeaderValue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler? handler)
        {
            // Read remaining chars, up to the length of the current data
            var count = Math.Min(_stringLength - _stringIndex, data.Length - currentIndex);

            data.Slice(currentIndex, count).CopyTo(_stringOctets.AsSpan(_stringIndex));
            _stringIndex += count;
            currentIndex += count;

            if (_stringIndex == _stringLength)
            {
                ProcessHeaderValue(handler);
            }
        }

        private void CheckIncompleteHeaderBlock(bool endHeaders)
        {
            if (endHeaders)
            {
                if (_state != State.Ready)
                {
                    throw new HPackDecodingException(SR.net_http_hpack_incomplete_header_block);
                }

                _headersObserved = false;
            }
        }

        private void ProcessHeaderValue(IHttpHeadersHandler? handler)
        {
            OnString(nextState: State.Ready);

            var headerNameSpan = new Span<byte>(_headerName, 0, _headerNameLength);
            var headerValueSpan = new Span<byte>(_headerValueOctets, 0, _headerValueLength);

            handler?.OnHeader(headerNameSpan, headerValueSpan);

            if (_index)
            {
                _dynamicTable.Insert(headerNameSpan, headerValueSpan);
            }
        }

        public void CompleteDecode()
        {
            if (_state != State.Ready)
            {
                // Incomplete header block
                throw new HPackDecodingException(SR.net_http_hpack_unexpected_end);
            }
        }

        private void OnIndexedHeaderField(int index, IHttpHeadersHandler? handler)
        {
            HeaderField header = GetHeader(index);
            handler?.OnHeader(header.Name, header.Value);
            _state = State.Ready;
        }

        private void OnIndexedHeaderName(int index)
        {
            HeaderField header = GetHeader(index);
            _headerName = header.Name;
            _headerNameLength = header.Name.Length;
            _state = State.HeaderValueLength;
        }

        private void OnStringLength(int length, State nextState)
        {
            if (length > _stringOctets.Length)
            {
                if (length > _maxHeadersLength)
                {
                    throw new HPackDecodingException(SR.Format(SR.net_http_headers_exceeded_length, _maxHeadersLength));
                }

                _stringOctets = new byte[Math.Max(length, _stringOctets.Length * 2)];
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void OnString(State nextState)
        {
            int Decode(ref byte[] dst)
            {
                if (_huffman)
                {
                    return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), ref dst);
                }
                else
                {
                    if (dst.Length < _stringLength)
                    {
                        dst = new byte[Math.Max(_stringLength, dst.Length * 2)];
                    }

                    Buffer.BlockCopy(_stringOctets, 0, dst, 0, _stringLength);
                    return _stringLength;
                }
            }

            try
            {
                if (_state == State.HeaderName)
                {
                    _headerNameLength = Decode(ref _headerNameOctets);
                    _headerName = _headerNameOctets;
                }
                else
                {
                    _headerValueLength = Decode(ref _headerValueOctets);
                }
            }
            catch (HuffmanDecodingException ex)
            {
                throw new HPackDecodingException(SR.net_http_hpack_huffman_decode_failed, ex);
            }

            _state = nextState;
        }

        private HeaderField GetHeader(int index)
        {
            try
            {
                return index <= H2StaticTable.Count
                    ? H2StaticTable.Get(index - 1)
                    : _dynamicTable[index - H2StaticTable.Count - 1];
            }
            catch (IndexOutOfRangeException)
            {
                // Header index out of range.
                throw new HPackDecodingException(SR.Format(SR.net_http_hpack_invalid_index, index));
            }
        }

        private void SetDynamicHeaderTableSize(int size)
        {
            if (size > _maxDynamicTableSize)
            {
                throw new HPackDecodingException(SR.Format(SR.net_http_hpack_large_table_size_update, size, _maxDynamicTableSize));
            }

            _dynamicTable.Resize(size);
        }
    }
}
