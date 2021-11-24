// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.Diagnostics;
using System.Net.Http.HPack;
using System.Numerics;

#if KESTREL
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
#endif

namespace System.Net.Http.QPack
{
    internal sealed class QPackDecoder : IDisposable
    {
        private enum State
        {
            RequiredInsertCount,
            RequiredInsertCountContinue,
            Base,
            BaseContinue,
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
        private const byte IndexedHeaderStaticMask = 0x40;
        private const byte IndexedHeaderStaticRepresentation = 0x40;
        private const byte IndexedHeaderFieldPrefixMask = 0x3F;
        private const int IndexedHeaderFieldPrefix = 6;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 0 | 0 | 1 |  Index(4+)   |
        //+---+---+---+---+---------------+
        private const byte PostBaseIndexMask = 0xF0;
        private const int PostBaseIndexPrefix = 4;

        //0   1   2   3   4   5   6   7
        //+---+---+---+---+---+---+---+---+
        //| 0 | 1 | N | S |Name Index(4+)|
        //+---+---+---+---+---------------+
        //| H |     Value Length(7+)     |
        //+---+---------------------------+
        //|  Value String(Length bytes)  |
        //+-------------------------------+
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
        private const byte LiteralHeaderFieldWithoutNameReferenceHuffmanMask = 0x08;
        private const byte LiteralHeaderFieldWithoutNameReferencePrefixMask = 0x07;
        private const int LiteralHeaderFieldWithoutNameReferencePrefix = 3;

        private const int StringLengthPrefix = 7;
        private const byte HuffmanMask = 0x80;

        private const int DefaultStringBufferSize = 64;

        private readonly int _maxHeadersLength;
        private State _state = State.RequiredInsertCount;

        private byte[] _stringOctets;
        private byte[] _headerNameOctets;
        private byte[] _headerValueOctets;

        // s is used for whatever s is in each field. This has multiple definition
        private bool _huffman;
        private int? _index;

        private byte[]? _headerName;
        private int _headerNameLength;
        private int _headerValueLength;
        private int _stringLength;
        private int _stringIndex;
        private IntegerDecoder _integerDecoder;

        private static ArrayPool<byte> Pool => ArrayPool<byte>.Shared;

        private static void ReturnAndGetNewPooledArray(ref byte[] buffer, int newSize)
        {
            byte[] old = buffer;
            buffer = null!;

            Pool.Return(old, clearArray: true);
            buffer = Pool.Rent(newSize);
        }

        public QPackDecoder(int maxHeadersLength)
        {
            _maxHeadersLength = maxHeadersLength;

            // TODO: make allocation lazy? with static entries it's possible no buffers will be needed.
            _stringOctets = Pool.Rent(DefaultStringBufferSize);
            _headerNameOctets = Pool.Rent(DefaultStringBufferSize);
            _headerValueOctets = Pool.Rent(DefaultStringBufferSize);
        }

        public void Dispose()
        {
            if (_stringOctets != null)
            {
                Pool.Return(_stringOctets, true);
                _stringOctets = null!;
            }

            if (_headerNameOctets != null)
            {
                Pool.Return(_headerNameOctets, true);
                _headerNameOctets = null!;
            }

            if (_headerValueOctets != null)
            {
                Pool.Return(_headerValueOctets, true);
                _headerValueOctets = null!;
            }
        }

        /// <summary>
        /// Reset the decoder state back to its initial value. Resetting state is required when reusing a decoder with multiple
        /// header frames. For example, decoding a response's headers and trailers.
        /// </summary>
        public void Reset()
        {
            _state = State.RequiredInsertCount;
        }

        public void Decode(in ReadOnlySequence<byte> headerBlock, IHttpHeadersHandler handler)
        {
            foreach (ReadOnlyMemory<byte> segment in headerBlock)
            {
                Decode(segment.Span, handler);
            }
        }

        public void Decode(ReadOnlySpan<byte> headerBlock, IHttpHeadersHandler handler)
        {
            foreach (byte b in headerBlock)
            {
                OnByte(b, handler);
            }
        }

        private void OnByte(byte b, IHttpHeadersHandler handler)
        {
            int intResult;
            int prefixInt;
            switch (_state)
            {
                case State.RequiredInsertCount:
                    if (_integerDecoder.BeginTryDecode(b, RequiredInsertCountPrefix, out intResult))
                    {
                        OnRequiredInsertCount(intResult);
                    }
                    else
                    {
                        _state = State.RequiredInsertCountContinue;
                    }
                    break;
                case State.RequiredInsertCountContinue:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnRequiredInsertCount(intResult);
                    }
                    break;
                case State.Base:
                    prefixInt = ~BaseMask & b;

                    if (_integerDecoder.BeginTryDecode(b, BasePrefix, out intResult))
                    {
                        OnBase(intResult);
                    }
                    else
                    {
                        _state = State.BaseContinue;
                    }
                    break;
                case State.BaseContinue:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        OnBase(intResult);
                    }
                    break;
                case State.CompressedHeaders:
                    switch (BitOperations.LeadingZeroCount(b) - 24) // byte 'b' is extended to uint, so will have 24 extra 0s.
                    {
                        case 0: // Indexed Header Field
                            prefixInt = IndexedHeaderFieldPrefixMask & b;

                            bool useStaticTable = (b & IndexedHeaderStaticMask) == IndexedHeaderStaticRepresentation;

                            if (!useStaticTable)
                            {
                                ThrowDynamicTableNotSupported();
                            }

                            if (_integerDecoder.BeginTryDecode((byte)prefixInt, IndexedHeaderFieldPrefix, out intResult))
                            {
                                OnIndexedHeaderField(intResult, handler);
                            }
                            else
                            {
                                _state = State.HeaderFieldIndex;
                            }
                            break;
                        case 1: // Literal Header Field With Name Reference
                            useStaticTable = (LiteralHeaderFieldStaticMask & b) == LiteralHeaderFieldStaticMask;

                            if (!useStaticTable)
                            {
                                ThrowDynamicTableNotSupported();
                            }

                            prefixInt = b & LiteralHeaderFieldPrefixMask;
                            if (_integerDecoder.BeginTryDecode((byte)prefixInt, LiteralHeaderFieldPrefix, out intResult))
                            {
                                OnIndexedHeaderName(intResult);
                            }
                            else
                            {
                                _state = State.HeaderNameIndex;
                            }
                            break;
                        case 2: // Literal Header Field Without Name Reference
                            _huffman = (b & LiteralHeaderFieldWithoutNameReferenceHuffmanMask) != 0;
                            prefixInt = b & LiteralHeaderFieldWithoutNameReferencePrefixMask;

                            if (_integerDecoder.BeginTryDecode((byte)prefixInt, LiteralHeaderFieldWithoutNameReferencePrefix, out intResult))
                            {
                                if (intResult == 0)
                                {
                                    throw new QPackDecodingException(SR.Format(SR.net_http_invalid_header_name, ""));
                                }
                                OnStringLength(intResult, State.HeaderName);
                            }
                            else
                            {
                                _state = State.HeaderNameLength;
                            }
                            break;
                        case 3: // Indexed Header Field With Post-Base Index
                            prefixInt = ~PostBaseIndexMask & b;
                            if (_integerDecoder.BeginTryDecode((byte)prefixInt, PostBaseIndexPrefix, out intResult))
                            {
                                OnPostBaseIndex(intResult, handler);
                            }
                            else
                            {
                                _state = State.PostBaseIndex;
                            }
                            break;
                        default: // Literal Header Field With Post-Base Name Reference (at least 4 zeroes, maybe more)
                            prefixInt = b & LiteralHeaderFieldPostBasePrefixMask;
                            if (_integerDecoder.BeginTryDecode((byte)prefixInt, LiteralHeaderFieldPostBasePrefix, out intResult))
                            {
                                OnIndexedHeaderNamePostBase(intResult);
                            }
                            else
                            {
                                _state = State.HeaderNameIndexPostBase;
                            }
                            break;
                    }
                    break;
                case State.HeaderNameLength:
                    if (_integerDecoder.TryDecode(b, out intResult))
                    {
                        if (intResult == 0)
                        {
                            throw new QPackDecodingException(SR.Format(SR.net_http_invalid_header_name, ""));
                        }
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
                if (length > _maxHeadersLength)
                {
                    throw new QPackDecodingException(SR.Format(SR.net_http_headers_exceeded_length, _maxHeadersLength));
                }

                ReturnAndGetNewPooledArray(ref _stringOctets, length);
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void ProcessHeaderValue(IHttpHeadersHandler handler)
        {
            OnString(nextState: State.CompressedHeaders);

            Span<byte> headerNameSpan;
            Span<byte> headerValueSpan = _headerValueOctets.AsSpan(0, _headerValueLength);

            if (_index is int index)
            {
                Debug.Assert(index >= 0 && index <= H3StaticTable.Count, $"The index should be a valid static index here. {nameof(QPackDecoder)} should have previously thrown if it read a dynamic index.");
                handler.OnStaticIndexedHeader(index, headerValueSpan);
                _index = null;

                return;
            }
            else
            {
                headerNameSpan = _headerNameOctets.AsSpan(0, _headerNameLength);
            }

            handler.OnHeader(headerNameSpan, headerValueSpan);
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
                        ReturnAndGetNewPooledArray(ref dst, _stringLength);
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
                throw new QPackDecodingException(SR.net_http_hpack_huffman_decode_failed, ex);
            }

            _state = nextState;
        }


        private void OnIndexedHeaderName(int index)
        {
            _index = index;
            _state = State.HeaderValueLength;
        }

        private void OnIndexedHeaderNamePostBase(int index)
        {
            ThrowDynamicTableNotSupported();
            // TODO update with postbase index
            // _index = index;
            // _state = State.HeaderValueLength;
        }

        private void OnPostBaseIndex(int intResult, IHttpHeadersHandler handler)
        {
            ThrowDynamicTableNotSupported();
            // TODO
            // _state = State.CompressedHeaders;
        }

        private void OnBase(int deltaBase)
        {
            if (deltaBase != 0)
            {
                ThrowDynamicTableNotSupported();
            }
            _state = State.CompressedHeaders;
        }

        private void OnRequiredInsertCount(int requiredInsertCount)
        {
            if (requiredInsertCount != 0)
            {
                ThrowDynamicTableNotSupported();
            }
            _state = State.Base;
        }

        private void OnIndexedHeaderField(int index, IHttpHeadersHandler handler)
        {
            handler.OnStaticIndexedHeader(index);
            _state = State.CompressedHeaders;
        }

        private static void ThrowDynamicTableNotSupported()
        {
            throw new QPackDecodingException(SR.net_http_qpack_no_dynamic_table);
        }
    }
}
