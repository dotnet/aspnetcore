// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            HeaderName,
            HeaderValueLength,
            HeaderValueLengthContinue,
            HeaderValue,
            PostBaseIndex,
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

        private const int HeaderStaticIndexUnset = -1; // Static index starts at 0

        private readonly int _maxHeadersLength;
        private State _state = State.RequiredInsertCount;

        // s is used for whatever s is in each field. This has multiple definition
        private bool _huffman;

        private byte[]? _headerName;
        private int _headerStaticIndex;
        private int _headerNameLength;
        private int _headerValueLength;
        private int _stringLength;
        private int _stringIndex;

        private IntegerDecoder _integerDecoder;
        private byte[]? _stringOctets;
        private byte[]? _headerNameOctets;
        private byte[]? _headerValueOctets;
        private (int start, int length)? _headerNameRange;
        private (int start, int length)? _headerValueRange;

        private static ArrayPool<byte> Pool => ArrayPool<byte>.Shared;

        public QPackDecoder(int maxHeadersLength)
        {
            _maxHeadersLength = maxHeadersLength;
            _headerStaticIndex = HeaderStaticIndexUnset;
        }

        public void Dispose()
        {
            if (_stringOctets != null)
            {
                Pool.Return(_stringOctets);
                _stringOctets = null!;
            }

            if (_headerNameOctets != null)
            {
                Pool.Return(_headerNameOctets);
                _headerNameOctets = null!;
            }

            if (_headerValueOctets != null)
            {
                Pool.Return(_headerValueOctets);
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

        public void Decode(in ReadOnlySequence<byte> data, bool endHeaders, IHttpStreamHeadersHandler handler)
        {
            foreach (ReadOnlyMemory<byte> segment in data)
            {
                DecodeInternal(segment.Span, handler);
            }
            CheckIncompleteHeaderBlock(endHeaders);
        }

        public void Decode(ReadOnlySpan<byte> data, bool endHeaders, IHttpStreamHeadersHandler handler)
        {
            DecodeInternal(data, handler);
            CheckIncompleteHeaderBlock(endHeaders);
        }

        private void DecodeInternal(ReadOnlySpan<byte> data, IHttpStreamHeadersHandler handler)
        {
            int currentIndex = 0;

            do
            {
                switch (_state)
                {
                    case State.RequiredInsertCount:
                        ParseRequiredInsertCount(data, ref currentIndex, handler);
                        break;
                    case State.RequiredInsertCountContinue:
                        ParseRequiredInsertCountContinue(data, ref currentIndex, handler);
                        break;
                    case State.Base:
                        ParseBase(data, ref currentIndex, handler);
                        break;
                    case State.BaseContinue:
                        ParseBaseContinue(data, ref currentIndex, handler);
                        break;
                    case State.CompressedHeaders:
                        ParseCompressedHeaders(data, ref currentIndex, handler);
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
                    case State.PostBaseIndex:
                        ParsePostBaseIndex(data, ref currentIndex);
                        break;
                    case State.HeaderNameIndexPostBase:
                        ParseHeaderNameIndexPostBase(data, ref currentIndex);
                        break;
                    default:
                        // Can't happen
                        Debug.Fail("QPACK decoder reach an invalid state");
                        throw new NotImplementedException(_state.ToString());
                }
            }
            // Parse methods each check the length. This check is to see whether there is still data available
            // and to continue parsing.
            while (currentIndex < data.Length);

            // If a header range was set, but the value was not in the data, then copy the range
            // to the name buffer. Must copy because the data will be replaced and the range
            // will no longer be valid.
            if (_headerNameRange != null)
            {
                EnsureStringCapacity(ref _headerNameOctets, _headerNameLength, existingLength: 0);
                _headerName = _headerNameOctets;

                ReadOnlySpan<byte> headerBytes = data.Slice(_headerNameRange.GetValueOrDefault().start, _headerNameRange.GetValueOrDefault().length);
                headerBytes.CopyTo(_headerName);
                _headerNameRange = null;
            }
        }

        private void ParseHeaderNameIndexPostBase(ReadOnlySpan<byte> data, ref int currentIndex)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                OnIndexedHeaderNamePostBase(intResult);
            }
        }

        private void ParsePostBaseIndex(ReadOnlySpan<byte> data, ref int currentIndex)
        {
            if (TryDecodeInteger(data, ref currentIndex, out _))
            {
                OnPostBaseIndex();
            }
        }

        private void ParseHeaderNameLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                if (intResult == 0)
                {
                    throw new QPackDecodingException(SR.Format(SR.net_http_invalid_header_name, ""));
                }
                OnStringLength(intResult, nextState: State.HeaderName);
                ParseHeaderName(data, ref currentIndex, handler);
            }
        }

        private void ParseHeaderName(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            // Read remaining chars, up to the length of the current data
            int count = Math.Min(_stringLength - _stringIndex, data.Length - currentIndex);

            // Check whether the whole string is available in the data and no decompression required.
            // If string is good then mark its range.
            // NOTE: it may need to be copied to buffer later the if value is not current data.
            if (count == _stringLength && !_huffman)
            {
                // Fast path. Store the range rather than copying.
                _headerNameRange = (start: currentIndex, count);
                _headerNameLength = _stringLength;
                currentIndex += count;

                _state = State.HeaderValueLength;
                ParseHeaderValueLength(data, ref currentIndex, handler);
            }
            else if (count == 0)
            {
                // no-op
            }
            else
            {
                // Copy string to temporary buffer.
                EnsureStringCapacity(ref _stringOctets, _stringIndex + count, existingLength: _stringIndex);
                data.Slice(currentIndex, count).CopyTo(_stringOctets.AsSpan(_stringIndex));

                _stringIndex += count;
                currentIndex += count;

                if (_stringIndex == _stringLength)
                {
                    OnString(nextState: State.HeaderValueLength);
                    ParseHeaderValueLength(data, ref currentIndex, handler);
                }
            }
        }

        private void ParseHeaderFieldIndex(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                OnIndexedHeaderField(intResult, handler);
            }
        }

        private void ParseHeaderNameIndex(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                OnIndexedHeaderName(intResult);
                ParseHeaderValueLength(data, ref currentIndex, handler);
            }
        }

        private void ParseHeaderValueLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (currentIndex < data.Length)
            {
                byte b = data[currentIndex++];

                _huffman = IsHuffmanEncoded(b);

                if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out int intResult))
                {
                    OnStringLength(intResult, nextState: State.HeaderValue);

                    if (intResult == 0)
                    {
                        _state = State.CompressedHeaders;
                        ProcessHeaderValue(data, handler);
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

        private void ParseHeaderValue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            // Read remaining chars, up to the length of the current data
            int count = Math.Min(_stringLength - _stringIndex, data.Length - currentIndex);

            // Check whether the whole string is available in the data and no decompressed required.
            // If string is good then mark its range.
            if (count == _stringLength && !_huffman)
            {
                // Fast path. Store the range rather than copying.
                _headerValueRange = (start: currentIndex, count);
                currentIndex += count;

                _state = State.CompressedHeaders;
                ProcessHeaderValue(data, handler);
            }
            else if (count == 0)
            {
                // no-op
            }
            else
            {
                // Copy string to temporary buffer.
                EnsureStringCapacity(ref _stringOctets, _stringIndex + count, existingLength: _stringIndex);
                data.Slice(currentIndex, count).CopyTo(_stringOctets.AsSpan(_stringIndex));

                _stringIndex += count;
                currentIndex += count;

                if (_stringIndex == _stringLength)
                {
                    OnString(nextState: State.CompressedHeaders);
                    ProcessHeaderValue(data, handler);
                }
            }
        }

        private void ParseHeaderValueLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                if (intResult == 0)
                {
                    _state = State.CompressedHeaders;
                    ProcessHeaderValue(data, handler);
                }
                else
                {
                    OnStringLength(intResult, nextState: State.HeaderValue);
                    ParseHeaderValue(data, ref currentIndex, handler);
                }
            }
        }

        private void ParseCompressedHeaders(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (currentIndex < data.Length)
            {
                Debug.Assert(_state == State.CompressedHeaders, "Should be ready to parse a new header.");

                byte b = data[currentIndex++];
                int prefixInt;
                int intResult;

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
                            ParseHeaderFieldIndex(data, ref currentIndex, handler);
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
                            ParseHeaderValueLength(data, ref currentIndex, handler);
                        }
                        else
                        {
                            _state = State.HeaderNameIndex;
                            ParseHeaderNameIndex(data, ref currentIndex, handler);
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
                            ParseHeaderName(data, ref currentIndex, handler);
                        }
                        else
                        {
                            _state = State.HeaderNameLength;
                            ParseHeaderNameLength(data, ref currentIndex, handler);
                        }
                        break;
                    case 3: // Indexed Header Field With Post-Base Index
                        prefixInt = ~PostBaseIndexMask & b;
                        if (_integerDecoder.BeginTryDecode((byte)prefixInt, PostBaseIndexPrefix, out _))
                        {
                            OnPostBaseIndex();
                        }
                        else
                        {
                            _state = State.PostBaseIndex;
                            ParsePostBaseIndex(data, ref currentIndex);
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
                            ParseHeaderNameIndexPostBase(data, ref currentIndex);
                        }
                        break;
                }
            }
        }

        private void ParseRequiredInsertCountContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                OnRequiredInsertCount(intResult);
                ParseBase(data, ref currentIndex, handler);
            }
        }

        private void ParseBase(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (currentIndex < data.Length)
            {
                byte b = data[currentIndex++];
                int prefixInt = ~BaseMask & b;

                if (_integerDecoder.BeginTryDecode((byte)prefixInt, BasePrefix, out int intResult))
                {
                    OnBase(intResult);
                    ParseCompressedHeaders(data, ref currentIndex, handler);
                }
                else
                {
                    _state = State.BaseContinue;
                    ParseBaseContinue(data, ref currentIndex, handler);
                }
            }
        }

        private void ParseBaseContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                OnBase(intResult);
                ParseCompressedHeaders(data, ref currentIndex, handler);
            }
        }

        private void ParseRequiredInsertCount(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (currentIndex < data.Length)
            {
                byte b = data[currentIndex++];

                if (_integerDecoder.BeginTryDecode(b, RequiredInsertCountPrefix, out int intResult))
                {
                    OnRequiredInsertCount(intResult);
                    ParseBase(data, ref currentIndex, handler);
                }
                else
                {
                    _state = State.RequiredInsertCountContinue;
                    ParseRequiredInsertCountContinue(data, ref currentIndex, handler);
                }
            }
        }

        private void CheckIncompleteHeaderBlock(bool endHeaders)
        {
            if (endHeaders)
            {
                if (_state != State.CompressedHeaders)
                {
                    throw new QPackDecodingException(SR.net_http_hpack_incomplete_header_block);
                }
            }
        }

        private void ProcessHeaderValue(ReadOnlySpan<byte> data, IHttpStreamHeadersHandler handler)
        {
            ReadOnlySpan<byte> headerValueSpan = _headerValueRange == null
                ? _headerValueOctets.AsSpan(0, _headerValueLength)
                : data.Slice(_headerValueRange.GetValueOrDefault().start, _headerValueRange.GetValueOrDefault().length);

            if (_headerStaticIndex != HeaderStaticIndexUnset)
            {
                handler.OnStaticIndexedHeader(_headerStaticIndex, headerValueSpan);
            }
            else
            {
                ReadOnlySpan<byte> headerNameSpan = _headerNameRange == null
                    ? _headerName.AsSpan(0, _headerNameLength)
                    : data.Slice(_headerNameRange.GetValueOrDefault().start, _headerNameRange.GetValueOrDefault().length);

                handler.OnHeader(headerNameSpan, headerValueSpan);
            }

            _headerStaticIndex = HeaderStaticIndexUnset;
            _headerNameRange = null;
            _headerNameLength = 0;
            _headerValueRange = null;
            _headerValueLength = 0;
        }

        private void OnStringLength(int length, State nextState)
        {
            if (length > _maxHeadersLength)
            {
                throw new QPackDecodingException(SR.Format(SR.net_http_headers_exceeded_length, _maxHeadersLength));
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void OnString(State nextState)
        {
            int Decode(ref byte[]? dst)
            {
                EnsureStringCapacity(ref dst, _stringLength, existingLength: 0);

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

            Debug.Assert(_stringOctets != null, "String buffer should have a value.");

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

        private static void EnsureStringCapacity([NotNull] ref byte[]? buffer, int requiredLength, int existingLength)
        {
            if (buffer == null)
            {
                buffer = Pool.Rent(requiredLength);
            }
            else if (buffer.Length < requiredLength)
            {
                byte[] newBuffer = Pool.Rent(requiredLength);
                if (existingLength > 0)
                {
                    buffer.AsMemory(0, existingLength).CopyTo(newBuffer);
                }

                Pool.Return(buffer);
                buffer = newBuffer;
            }
        }

        private bool TryDecodeInteger(ReadOnlySpan<byte> data, ref int currentIndex, out int result)
        {
            for (; currentIndex < data.Length; currentIndex++)
            {
                if (_integerDecoder.TryDecode(data[currentIndex], out result))
                {
                    currentIndex++;
                    return true;
                }
            }

            result = default;
            return false;
        }

        private static bool IsHuffmanEncoded(byte b)
        {
            return (b & HuffmanMask) != 0;
        }

        private void OnIndexedHeaderName(int index)
        {
            _headerStaticIndex = index;
            _state = State.HeaderValueLength;
        }

        private static void OnIndexedHeaderNamePostBase(int _ /*index*/)
        {
            ThrowDynamicTableNotSupported();
            // TODO update with postbase index
            // _index = index;
            // _state = State.HeaderValueLength;
        }

        private static void OnPostBaseIndex()
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

        private void OnIndexedHeaderField(int index, IHttpStreamHeadersHandler handler)
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
