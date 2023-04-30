// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
#if KESTREL
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
#endif

namespace System.Net.Http.HPack
{
    internal sealed class HPackDecoder
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

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.1
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 1 |      Index (6+)       |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldWithIncrementalIndexingMask = 0xc0;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.2
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 0 | 0 |  Index (4+)   |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldWithoutIndexingMask = 0xf0;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.3
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 0 | 1 |  Index (4+)   |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldNeverIndexedMask = 0xf0;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.3
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 1 |   Max size (5+)   |
        // +---+---------------------------+
        private const byte DynamicTableSizeUpdateMask = 0xe0;

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
        private IntegerDecoder _integerDecoder;
        private byte[] _stringOctets;
        private byte[] _headerNameOctets;
        private byte[] _headerValueOctets;
        private (int start, int length)? _headerNameRange;
        private (int start, int length)? _headerValueRange;

        private State _state = State.Ready;
        private byte[]? _headerName;
        private int _headerStaticIndex;
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
            // Parse methods each check the length. This check is to see whether there is still data available
            // and to continue parsing.
            while (currentIndex < data.Length);

            // If a header range was set, but the value was not in the data, then copy the range
            // to the name buffer. Must copy because the data will be replaced and the range
            // will no longer be valid.
            if (_headerNameRange != null)
            {
                EnsureStringCapacity(ref _headerNameOctets, _headerNameLength);
                _headerName = _headerNameOctets;

                ReadOnlySpan<byte> headerBytes = data.Slice(_headerNameRange.GetValueOrDefault().start, _headerNameRange.GetValueOrDefault().length);
                headerBytes.CopyTo(_headerName);
                _headerNameRange = null;
            }
        }

        private void ParseDynamicTableSizeUpdate(ReadOnlySpan<byte> data, ref int currentIndex)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                SetDynamicHeaderTableSize(intResult);
                _state = State.Ready;
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
                        OnString(nextState: State.Ready);
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

        private void ParseHeaderNameLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                // IntegerDecoder disallows overlong encodings, where an integer is encoded with more bytes than is strictly required.
                // 0 should always be represented by a single byte, so we shouldn't need to check for it in the continuation case.
                Debug.Assert(intResult != 0, "A header name length of 0 should never be encoded with a continuation byte.");

                OnStringLength(intResult, nextState: State.HeaderName);
                ParseHeaderName(data, ref currentIndex, handler);
            }
        }

        private void ParseHeaderValueLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (TryDecodeInteger(data, ref currentIndex, out int intResult))
            {
                // 0 should always be represented by a single byte, so we shouldn't need to check for it in the continuation case.
                Debug.Assert(intResult != 0, "A header value length of 0 should never be encoded with a continuation byte.");

                OnStringLength(intResult, nextState: State.HeaderValue);
                ParseHeaderValue(data, ref currentIndex, handler);
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

        private void ParseHeaderNameLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (currentIndex < data.Length)
            {
                byte b = data[currentIndex++];

                _huffman = IsHuffmanEncoded(b);

                if (_integerDecoder.BeginTryDecode((byte)(b & ~HuffmanMask), StringLengthPrefix, out int intResult))
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

        private void Parse(ReadOnlySpan<byte> data, ref int currentIndex, IHttpStreamHeadersHandler handler)
        {
            if (currentIndex < data.Length)
            {
                Debug.Assert(_state == State.Ready, "Should be ready to parse a new header.");

                byte b = data[currentIndex++];

                switch (BitOperations.LeadingZeroCount(b) - 24) // byte 'b' is extended to uint, so will have 24 extra 0s.
                {
                    case 0: // Indexed Header Field
                        {
                            _headersObserved = true;

                            int val = b & ~IndexedHeaderFieldMask;

                            if (_integerDecoder.BeginTryDecode((byte)val, IndexedHeaderFieldPrefix, out int intResult))
                            {
                                OnIndexedHeaderField(intResult, handler);
                            }
                            else
                            {
                                _state = State.HeaderFieldIndex;
                                ParseHeaderFieldIndex(data, ref currentIndex, handler);
                            }
                            break;
                        }
                    case 1: // Literal Header Field with Incremental Indexing
                        ParseLiteralHeaderField(
                            data,
                            ref currentIndex,
                            b,
                            LiteralHeaderFieldWithIncrementalIndexingMask,
                            LiteralHeaderFieldWithIncrementalIndexingPrefix,
                            index: true,
                            handler);
                        break;
                    case 4:
                    default: // Literal Header Field without Indexing
                        ParseLiteralHeaderField(
                            data,
                            ref currentIndex,
                            b,
                            LiteralHeaderFieldWithoutIndexingMask,
                            LiteralHeaderFieldWithoutIndexingPrefix,
                            index: false,
                            handler);
                        break;
                    case 3: // Literal Header Field Never Indexed
                        ParseLiteralHeaderField(
                            data,
                            ref currentIndex,
                            b,
                            LiteralHeaderFieldNeverIndexedMask,
                            LiteralHeaderFieldNeverIndexedPrefix,
                            index: false,
                            handler);
                        break;
                    case 2: // Dynamic Table Size Update
                        {
                            // https://tools.ietf.org/html/rfc7541#section-4.2
                            // This dynamic table size
                            // update MUST occur at the beginning of the first header block
                            // following the change to the dynamic table size.
                            if (_headersObserved)
                            {
                                throw new HPackDecodingException(SR.net_http_hpack_late_dynamic_table_size_update);
                            }

                            if (_integerDecoder.BeginTryDecode((byte)(b & ~DynamicTableSizeUpdateMask), DynamicTableSizeUpdatePrefix, out int intResult))
                            {
                                SetDynamicHeaderTableSize(intResult);
                            }
                            else
                            {
                                _state = State.DynamicTableSizeUpdate;
                                ParseDynamicTableSizeUpdate(data, ref currentIndex);
                            }
                            break;
                        }
                }
            }
        }

        private void ParseLiteralHeaderField(ReadOnlySpan<byte> data, ref int currentIndex, byte b, byte mask, byte indexPrefix, bool index, IHttpStreamHeadersHandler handler)
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
                if (_integerDecoder.BeginTryDecode((byte)val, indexPrefix, out int intResult))
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
                // _stringOctets was already
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

                _state = State.Ready;
                ProcessHeaderValue(data, handler);
            }
            else
            {
                // Copy string to temporary buffer.
                data.Slice(currentIndex, count).CopyTo(_stringOctets.AsSpan(_stringIndex));
                _stringIndex += count;
                currentIndex += count;

                if (_stringIndex == _stringLength)
                {
                    OnString(nextState: State.Ready);
                    ProcessHeaderValue(data, handler);
                }
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

        private void ProcessHeaderValue(ReadOnlySpan<byte> data, IHttpStreamHeadersHandler handler)
        {
            ReadOnlySpan<byte> headerValueSpan = _headerValueRange == null
                ? _headerValueOctets.AsSpan(0, _headerValueLength)
                : data.Slice(_headerValueRange.GetValueOrDefault().start, _headerValueRange.GetValueOrDefault().length);

            if (_headerStaticIndex > 0)
            {
                handler.OnStaticIndexedHeader(_headerStaticIndex, headerValueSpan);

                if (_index)
                {
                    _dynamicTable.Insert(_headerStaticIndex, H2StaticTable.Get(_headerStaticIndex - 1).Name, headerValueSpan);
                }
            }
            else
            {
                ReadOnlySpan<byte> headerNameSpan = _headerNameRange == null
                    ? _headerName.AsSpan(0, _headerNameLength)
                    : data.Slice(_headerNameRange.GetValueOrDefault().start, _headerNameRange.GetValueOrDefault().length);

                handler.OnHeader(headerNameSpan, headerValueSpan);

                if (_index)
                {
                    _dynamicTable.Insert(headerNameSpan, headerValueSpan);
                }
            }

            _headerStaticIndex = 0;
            _headerNameRange = null;
            _headerValueRange = null;
        }

        public void CompleteDecode()
        {
            if (_state != State.Ready)
            {
                // Incomplete header block
                throw new HPackDecodingException(SR.net_http_hpack_unexpected_end);
            }
        }

        private void OnIndexedHeaderField(int index, IHttpStreamHeadersHandler handler)
        {
            if (index <= H2StaticTable.Count)
            {
                handler.OnStaticIndexedHeader(index);
            }
            else
            {
                ref readonly HeaderField header = ref GetDynamicHeader(index);
                handler.OnDynamicIndexedHeader(header.StaticTableIndex, header.Name, header.Value);
            }

            _state = State.Ready;
        }

        private void OnIndexedHeaderName(int index)
        {
            if (index <= H2StaticTable.Count)
            {
                _headerStaticIndex = index;
            }
            else
            {
                _headerName = GetDynamicHeader(index).Name;
                _headerNameLength = _headerName.Length;
            }
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

                _stringOctets = new byte[Math.Max(length, Math.Min(_stringOctets.Length * 2, _maxHeadersLength))];
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
                    EnsureStringCapacity(ref dst);
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

        private void EnsureStringCapacity(ref byte[] dst, int stringLength = -1)
        {
            stringLength = stringLength >= 0 ? stringLength : _stringLength;
            if (dst.Length < stringLength)
            {
                dst = new byte[Math.Max(stringLength, Math.Min(dst.Length * 2, _maxHeadersLength))];
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

        private ref readonly HeaderField GetDynamicHeader(int index)
        {
            try
            {
                return ref _dynamicTable[index - H2StaticTable.Count - 1];
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
