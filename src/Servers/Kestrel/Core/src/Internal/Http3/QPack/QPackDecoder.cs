// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net.Http;
using System.Net.Http.HPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    internal class QPackDecoder
    {
        private enum State
        {
            Ready,
            RequiredInsertCount,
            RequiredInsertCountDone,
            Base,
            CompressedHeaders,
            HeaderFieldIndex,
            HeaderNameIndex,
            HeaderNameLength,
            HeaderNameLengthContinue,
            HeaderName,
            HeaderValueLength,
            HeaderValueLengthContinue,
            HeaderValue,
            DynamicTableSizeUpdate,
            PostBaseIndex,
            LiteralHeaderFieldWithNameReference,
            HeaderNameIndexPostBase
        }

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //|   Required Insert Count(8+)   |
        //+---+---------------------------+
        //| S |      Delta Base(7+)       |
        //+---+---------------------------+
        //|      Compressed Headers     ...
        private const int RequiredInsertCountPrefix = 8;
        private const int BaseMask = 0x80;
        private const int BasePrefix = 7;
        //+-------------------------------+

        //https://tools.ietf.org/html/draft-ietf-quic-qpack-09#section-4.5.2
        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 1 | S |      Index(6+)       |
        //+---+---+-----------------------+
        private const byte IndexedHeaderFieldMask = 0x80;
        private const byte IndexedHeaderFieldRepresentation = 0x80;
        private const byte IndexedHeaderStaticMask = 0x40;
        private const byte IndexedHeaderStaticRepresentation = 0x40;
        private const byte IndexedHeaderFieldPrefixMask = 0x3F;
        private const int IndexedHeaderFieldPrefix = 6;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 0 | 1 |  Index(4+)   |
        //+---+---+---+---+---------------+
        private const byte PostBaseIndexMask = 0xF0;
        private const byte PostBaseIndexRepresentation = 0x10;
        private const int PostBaseIndexPrefix = 4;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 1 | N | S |Name Index(4+)|
        //+---+---+---+---+---------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
        private const byte LiteralHeaderFieldMask = 0xC0;
        private const byte LiteralHeaderFieldRepresentation = 0x40;
        private const byte LiteralHeaderFieldNMask = 0x20;
        private const byte LiteralHeaderFieldStaticMask = 0x10;
        private const byte LiteralHeaderFieldPrefixMask = 0x0F;
        private const int LiteralHeaderFieldPrefix = 4;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 0 | 0 | N |NameIdx(3+)|
        //+---+---+---+---+---+-----------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
        private const byte LiteralHeaderFieldPostBaseMask = 0xF0;
        private const byte LiteralHeaderFieldPostBaseRepresentation = 0x00;
        private const byte LiteralHeaderFieldPostBaseNMask = 0x08;
        private const byte LiteralHeaderFieldPostBasePrefixMask = 0x07;
        private const int LiteralHeaderFieldPostBasePrefix = 3;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 1 | N | H |NameLen(3+)|
        //+---+---+---+---+---+-----------+
        //|  Name String(Length bytes)   |
        //+---+---------------------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
        private const byte LiteralHeaderFieldWithoutNameReferenceMask = 0xE0;
        private const byte LiteralHeaderFieldWithoutNameReferenceRepresentation = 0x20;
        private const byte LiteralHeaderFieldWithoutNameReferenceNMask = 0x10;
        private const byte LiteralHeaderFieldWithoutNameReferenceHuffmanMask = 0x08;
        private const byte LiteralHeaderFieldWithoutNameReferencePrefixMask = 0x07;
        private const int LiteralHeaderFieldWithoutNameReferencePrefix = 3;

        private const int StringLengthPrefix = 7;
        private const byte HuffmanMask = 0x80;

        private State _state = State.Ready;
        // TODO break out dynamic table entirely.
        private long _maxDynamicTableSize;
        private DynamicTable _dynamicTable;

        // TODO idk what these are for.
        private byte[] _stringOctets;
        private byte[] _headerNameOctets;
        private byte[] _headerValueOctets;
        private int _requiredInsertCount;
        //private int _insertCount;
        private int _base;

        // s is used for whatever s is in each field. This has multiple definition
        private bool _s;
        private bool _n;
        private bool _huffman;
        private bool _index;

        private byte[] _headerName;
        private int _headerNameLength;
        private int _headerValueLength;
        private int _stringLength;
        private int _stringIndex;
        private readonly IntegerDecoder _integerDecoder = new IntegerDecoder();

        // Decoders are on the http3stream now, each time we see a header block
        public QPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize)
            : this(maxDynamicTableSize, maxRequestHeaderFieldSize, new DynamicTable(maxDynamicTableSize)) { }


        // For testing.
        internal QPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize, DynamicTable dynamicTable)
        {
            _maxDynamicTableSize = maxDynamicTableSize;
            _dynamicTable = dynamicTable;

            _stringOctets = new byte[maxRequestHeaderFieldSize];
            _headerNameOctets = new byte[maxRequestHeaderFieldSize];
            _headerValueOctets = new byte[maxRequestHeaderFieldSize];
        }

        // sequence will probably be a header block instead.
        public void Decode(in ReadOnlySequence<byte> headerBlock, IHttpHeadersHandler handler)
        {
            // TODO I need to get the RequiredInsertCount and DeltaBase
            // These are always present in the header block
            // TODO need to figure out if I have read an entire header block.

            // (I think this can be done based on length outside of this)
            foreach (var segment in headerBlock)
            {
                var span = segment.Span;
                for (var i = 0; i < span.Length; i++)
                {
                    OnByte(span[i], handler);
                }
            }
        }

        private void OnByte(byte b, IHttpHeadersHandler handler)
        {
            int intResult;
            int prefixInt;
            switch (_state)
            {
                case State.Ready:
                    if (_integerDecoder.BeginTryDecode(b, RequiredInsertCountPrefix, out intResult))
                    {
                        OnRequiredInsertCount(intResult);
                    }
                    else
                    {
                        _state = State.RequiredInsertCount;
                    }
                    break;
                case State.RequiredInsertCount:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnRequiredInsertCount(intResult);
                    }
                    break;
                case State.RequiredInsertCountDone:
                    prefixInt = ~BaseMask & b;

                    _s = (b & BaseMask) == BaseMask;

                    if (_integerDecoder.BeginTryDecode(b, BasePrefix, out intResult))
                    {
                        OnBase(intResult);
                    }
                    else
                    {
                        _state = State.Base;
                    }
                    break;
                case State.Base:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnBase(intResult);
                    }
                    break;
                case State.CompressedHeaders:
                    if ((b & IndexedHeaderFieldMask) == IndexedHeaderFieldRepresentation)
                    {
                        prefixInt = IndexedHeaderFieldPrefixMask & b;
                        _s = (b & IndexedHeaderStaticMask) == IndexedHeaderStaticRepresentation;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, IndexedHeaderFieldPrefix, out intResult))
                        {
                            OnIndexedHeaderField(intResult, handler);
                        }
                        else
                        {
                            _state = State.HeaderFieldIndex;
                        }
                    }
                    else if ((b & PostBaseIndexMask) == PostBaseIndexRepresentation)
                    {
                        prefixInt = ~PostBaseIndexMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, PostBaseIndexPrefix, out intResult))
                        {
                            OnPostBaseIndex(intResult, handler);
                        }
                        else
                        {
                            _state = State.PostBaseIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldMask) == LiteralHeaderFieldRepresentation)
                    {
                        _index = true;
                        // Represents whether an intermediary is permitted to add this header to the dynamic header table on
                        // subsequent hops.
                        // if n is set, the encoded header must always be encoded with a literal representation

                        _n = (LiteralHeaderFieldNMask & b) == LiteralHeaderFieldNMask;
                        _s = (LiteralHeaderFieldStaticMask & b) == LiteralHeaderFieldStaticMask;
                        prefixInt = b & LiteralHeaderFieldPrefixMask;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, LiteralHeaderFieldPrefix, out intResult))
                        {
                            OnIndexedHeaderName(intResult);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                        }
                    }
                    else if ((b & LiteralHeaderFieldPostBaseMask) == LiteralHeaderFieldPostBaseRepresentation)
                    {
                        _index = true;
                        _n = (LiteralHeaderFieldPostBaseNMask & b) == LiteralHeaderFieldPostBaseNMask;
                        prefixInt = b & LiteralHeaderFieldPostBasePrefixMask;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, LiteralHeaderFieldPostBasePrefix, out intResult))
                        {
                            OnIndexedHeaderNamePostBase(intResult);
                        }
                        else
                        {
                            _state = State.HeaderNameIndexPostBase;
                        }
                    }
                    else if ((b & LiteralHeaderFieldWithoutNameReferenceMask) == LiteralHeaderFieldWithoutNameReferenceRepresentation)
                    {
                        _index = false;
                        _n = (LiteralHeaderFieldWithoutNameReferenceNMask & b) == LiteralHeaderFieldWithoutNameReferenceNMask;
                        _huffman = (b & LiteralHeaderFieldWithoutNameReferenceHuffmanMask) != 0;
                        prefixInt = b & LiteralHeaderFieldWithoutNameReferencePrefixMask;

                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, LiteralHeaderFieldWithoutNameReferencePrefix, out intResult))
                        {
                            OnStringLength(intResult, State.HeaderName);
                        }
                        else
                        {
                            _state = State.HeaderNameLength;
                        }
                    }
                    break;
                case State.HeaderNameLength:
                    // huffman has already been processed.
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
                case State.HeaderNameIndex:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnIndexedHeaderName(intResult);
                    }
                    break;
                case State.HeaderNameIndexPostBase:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnIndexedHeaderNamePostBase(intResult);
                    }
                    break;
                case State.HeaderValueLength:
                    _huffman = (b & HuffmanMask) != 0;

                    // TODO confirm this.
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
                case State.HeaderFieldIndex:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnIndexedHeaderField(intResult, handler);
                    }
                    break;
                case State.PostBaseIndex:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnPostBaseIndex(intResult, handler);
                    }
                    break;
                case State.LiteralHeaderFieldWithNameReference:
                    break;
            }
        }

        private void OnStringLength(int length, State nextState)
        {
            if (length > _stringOctets.Length)
            {
                throw new QPackDecodingException("TODO sync with corefx" /*CoreStrings.FormatQPackStringLengthTooLarge(length, _stringOctets.Length)*/);
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void ProcessHeaderValue(IHttpHeadersHandler handler)
        {
            OnString(nextState: State.CompressedHeaders);

            var headerNameSpan = new Span<byte>(_headerName, 0, _headerNameLength);
            var headerValueSpan = new Span<byte>(_headerValueOctets, 0, _headerValueLength);

            handler.OnHeader(headerNameSpan, headerValueSpan);

            if (_index)
            {
                _dynamicTable.Insert(headerNameSpan, headerValueSpan);
            }
        }

        private void OnString(State nextState)
        {
            int Decode(byte[] dst)
            {
                if (_huffman)
                {
                    return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), ref dst);
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
                throw new QPackDecodingException("TODO sync with corefx" /*CoreStrings.QPackHuffmanError, */, ex);
            }

            _state = nextState;
        }


        private void OnIndexedHeaderName(int index)
        {
            var header = GetHeader(index);
            _headerName = header.Name;
            _headerNameLength = header.Name.Length;
            _state = State.HeaderValueLength;
        }

        private void OnIndexedHeaderNamePostBase(int index)
        {
            // TODO update with postbase index
            var header = GetHeader(index);
            _headerName = header.Name;
            _headerNameLength = header.Name.Length;
            _state = State.HeaderValueLength;
        }

        private void OnPostBaseIndex(int intResult, IHttpHeadersHandler handler)
        {
            // TODO
            _state = State.CompressedHeaders;
        }

        private void OnBase(int deltaBase)
        {
            _state = State.CompressedHeaders;
            if (_s)
            {
                _base = _requiredInsertCount - deltaBase - 1;
            }
            else
            {
                _base = _requiredInsertCount + deltaBase;
            }
        }

        // TODO 
        private void OnRequiredInsertCount(int requiredInsertCount)
        {
            _requiredInsertCount = requiredInsertCount;
            _state = State.RequiredInsertCountDone;
            // This is just going to noop for now. I don't get this algorithm at all.
            //    var encoderInsertCount = 0;
            //    var maxEntries = _maxDynamicTableSize / HeaderField.RfcOverhead;

            //    if (requiredInsertCount != 0)
            //    {
            //        encoderInsertCount = (requiredInsertCount % ( 2 * maxEntries)) + 1;
            //    }

            //    // Dude I don't get this algorithm...
            //    var fullRange = 2 * maxEntries;
            //    if (encoderInsertCount == 0)
            //    {

            //    }
        }

        private void OnIndexedHeaderField(int index, IHttpHeadersHandler handler)
        {
            // Indexes start at 0 in QPack
            var header = GetHeader(index);
            handler.OnHeader(new Span<byte>(header.Name), new Span<byte>(header.Value));
            _state = State.CompressedHeaders;
        }

        private HeaderField GetHeader(int index)
        {
            try
            {
                return _s ? StaticTable.Instance[index] : _dynamicTable[index];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new QPackDecodingException("TODO sync with corefx" /*CoreStrings.FormatQPackErrorIndexOutOfRange(index),  */, ex);
            }
        }
    }
}
