// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.HPack;
using Xunit;
#if KESTREL
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
#endif

namespace System.Net.Http.Unit.Tests.HPack
{
    public class HPackDecoderTests
    {
        private const int DynamicTableInitialMaxSize = 4096;
        private const int MaxHeaderFieldSize = 8192;

        // Indexed Header Field Representation - Static Table - Index 2 (:method: GET)
        private static readonly byte[] _indexedHeaderStatic = new byte[] { 0x82 };

        // Indexed Header Field Representation - Dynamic Table - Index 62 (first index in dynamic table)
        private static readonly byte[] _indexedHeaderDynamic = new byte[] { 0xbe };

        // Literal Header Field with Incremental Indexing Representation - New Name
        private static readonly byte[] _literalHeaderFieldWithIndexingNewName = new byte[] { 0x40 };

        // Literal Header Field with Incremental Indexing Representation - Indexed Name - Index 58 (user-agent)
        private static readonly byte[] _literalHeaderFieldWithIndexingIndexedName = new byte[] { 0x7a };

        // Literal Header Field without Indexing Representation - New Name
        private static readonly byte[] _literalHeaderFieldWithoutIndexingNewName = new byte[] { 0x00 };

        // Literal Header Field without Indexing Representation - Indexed Name - Index 58 (user-agent)
        private static readonly byte[] _literalHeaderFieldWithoutIndexingIndexedName = new byte[] { 0x0f, 0x2b };

        // Literal Header Field Never Indexed Representation - New Name
        private static readonly byte[] _literalHeaderFieldNeverIndexedNewName = new byte[] { 0x10 };

        // Literal Header Field Never Indexed Representation - Indexed Name - Index 58 (user-agent)
        private static readonly byte[] _literalHeaderFieldNeverIndexedIndexedName = new byte[] { 0x1f, 0x2b };

        private const string _userAgentString = "user-agent";

        private const string _headerNameString = "new-header";

        private static readonly byte[] _headerNameBytes = Encoding.ASCII.GetBytes(_headerNameString);

        // n     e     w       -      h     e     a     d     e     r      *
        // 10101000 10111110 00010110 10011100 10100011 10010000 10110110 01111111
        private static readonly byte[] _headerNameHuffmanBytes = new byte[] { 0xa8, 0xbe, 0x16, 0x9c, 0xa3, 0x90, 0xb6, 0x7f };

        private const string _headerValueString = "value";

        private static readonly byte[] _headerValueBytes = Encoding.ASCII.GetBytes(_headerValueString);

        // v      a     l      u      e    *
        // 11101110 00111010 00101101 00101111
        private static readonly byte[] _headerValueHuffmanBytes = new byte[] { 0xee, 0x3a, 0x2d, 0x2f };

        private static readonly byte[] _headerName = new byte[] { (byte)_headerNameBytes.Length }
            .Concat(_headerNameBytes)
            .ToArray();

        private static readonly byte[] _headerNameHuffman = new byte[] { (byte)(0x80 | _headerNameHuffmanBytes.Length) }
            .Concat(_headerNameHuffmanBytes)
            .ToArray();

        private static readonly byte[] _headerValue = new byte[] { (byte)_headerValueBytes.Length }
            .Concat(_headerValueBytes)
            .ToArray();

        private static readonly byte[] _headerValueHuffman = new byte[] { (byte)(0x80 | _headerValueHuffmanBytes.Length) }
            .Concat(_headerValueHuffmanBytes)
            .ToArray();

        // &        *
        // 11111000 11111111
        private static readonly byte[] _huffmanLongPadding = new byte[] { 0x82, 0xf8, 0xff };

        // EOS                              *
        // 11111111 11111111 11111111 11111111
        private static readonly byte[] _huffmanEos = new byte[] { 0x84, 0xff, 0xff, 0xff, 0xff };

        private readonly DynamicTable _dynamicTable;
        private readonly HPackDecoder _decoder;
        private readonly TestHttpHeadersHandler _handler = new TestHttpHeadersHandler();

        public HPackDecoderTests()
        {
            (_dynamicTable, _decoder) = CreateDecoderAndTable();
        }

        private static (DynamicTable, HPackDecoder) CreateDecoderAndTable()
        {
            var dynamicTable = new DynamicTable(DynamicTableInitialMaxSize);
            var decoder = new HPackDecoder(DynamicTableInitialMaxSize, MaxHeaderFieldSize, dynamicTable);

            return (dynamicTable, decoder);
        }

        [Fact]
        public void DecodesIndexedHeaderField_StaticTableWithValue()
        {
            _decoder.Decode(_indexedHeaderStatic, endHeaders: true, handler: _handler);
            Assert.Equal("GET", _handler.DecodedHeaders[":method"]);

            Assert.Equal(":method", _handler.DecodedStaticHeaders[H2StaticTable.MethodGet].Key);
            Assert.Equal("GET", _handler.DecodedStaticHeaders[H2StaticTable.MethodGet].Value);
        }

        [Fact]
        public void DecodesIndexedHeaderField_StaticTableWithoutValue()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingIndexedName
                .Concat(_headerValue)
                .ToArray();

            _decoder.Decode(encoded, endHeaders: true, handler: _handler);
            Assert.Equal(_headerValueString, _handler.DecodedHeaders[_userAgentString]);

            Assert.Equal(_userAgentString, _handler.DecodedStaticHeaders[H2StaticTable.UserAgent].Key);
            Assert.Equal(_headerValueString, _handler.DecodedStaticHeaders[H2StaticTable.UserAgent].Value);
        }

        [Fact]
        public void DecodesIndexedHeaderField_DynamicTable()
        {
            // Add the header to the dynamic table
            _dynamicTable.Insert(_headerNameBytes, _headerValueBytes);

            // Index it
            _decoder.Decode(_indexedHeaderDynamic, endHeaders: true, handler: _handler);
            Assert.Equal(_headerValueString, _handler.DecodedHeaders[_headerNameString]);
        }

        [Fact]
        public void DecodesIndexedHeaderField_OutOfRange_Error()
        {
            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() =>
                _decoder.Decode(_indexedHeaderDynamic, endHeaders: true, handler: _handler));
            Assert.Equal(SR.Format(SR.net_http_hpack_invalid_index, 62), exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName_HuffmanEncodedName()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName_HuffmanEncodedValue()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName_HuffmanEncodedNameAndValue()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_IndexedName()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingIndexedName
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_IndexedName_HuffmanEncodedValue()
        {
            byte[] encoded = _literalHeaderFieldWithIndexingIndexedName
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_IndexedName_OutOfRange_Error()
        {
            // 01      (Literal Header Field without Indexing Representation)
            // 11 1110 (Indexed Name - Index 62 encoded with 6-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            // Index 62 is the first entry in the dynamic table. If there's nothing there, the decoder should throw.

            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new byte[] { 0x7e }, endHeaders: true, handler: _handler));
            Assert.Equal(SR.Format(SR.net_http_hpack_invalid_index, 62), exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName_HuffmanEncodedName()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName_HuffmanEncodedValue()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName_HuffmanEncodedNameAndValue()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_IndexedName()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingIndexedName
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_IndexedName_HuffmanEncodedValue()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingIndexedName
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_IndexedName_OutOfRange_Error()
        {
            // 0000           (Literal Header Field without Indexing Representation)
            // 1111 0010 1111 (Indexed Name - Index 62 encoded with 4-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            // Index 62 is the first entry in the dynamic table. If there's nothing there, the decoder should throw.

            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new byte[] { 0x0f, 0x2f }, endHeaders: true, handler: _handler));
            Assert.Equal(SR.Format(SR.net_http_hpack_invalid_index, 62), exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName()
        {
            byte[] encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_Duplicated()
        {
            byte[] encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            encoded = encoded.Concat(encoded).ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_HuffmanEncodedName()
        {
            byte[] encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_HuffmanEncodedValue()
        {
            byte[] encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerName)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_HuffmanEncodedNameAndValue()
        {
            byte[] encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_IndexedName()
        {
            // 0001           (Literal Header Field Never Indexed Representation)
            // 1111 0010 1011 (Indexed Name - Index 58 encoded with 4-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            // Concatenated with value bytes
            byte[] encoded = _literalHeaderFieldNeverIndexedIndexedName
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_IndexedName_HuffmanEncodedValue()
        {
            // 0001           (Literal Header Field Never Indexed Representation)
            // 1111 0010 1011 (Indexed Name - Index 58 encoded with 4-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            // Concatenated with Huffman encoded value bytes
            byte[] encoded = _literalHeaderFieldNeverIndexedIndexedName
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_IndexedName_OutOfRange_Error()
        {
            // 0001           (Literal Header Field Never Indexed Representation)
            // 1111 0010 1111 (Indexed Name - Index 62 encoded with 4-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            // Index 62 is the first entry in the dynamic table. If there's nothing there, the decoder should throw.

            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new byte[] { 0x1f, 0x2f }, endHeaders: true, handler: _handler));
            Assert.Equal(SR.Format(SR.net_http_hpack_invalid_index, 62), exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate()
        {
            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)

            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            _decoder.Decode(new byte[] { 0x3e }, endHeaders: true, handler: _handler);

            Assert.Equal(30, _dynamicTable.MaxSize);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_AfterIndexedHeaderStatic_Error()
        {
            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)

            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            byte[] data = _indexedHeaderStatic.Concat(new byte[] { 0x3e }).ToArray();
            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(data, endHeaders: true, handler: _handler));
            Assert.Equal(SR.net_http_hpack_late_dynamic_table_size_update, exception.Message);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_AfterIndexedHeaderStatic_SubsequentDecodeCall_Error()
        {
            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            _decoder.Decode(_indexedHeaderStatic, endHeaders: false, handler: _handler);
            Assert.Equal("GET", _handler.DecodedHeaders[":method"]);

            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            byte[] data = new byte[] { 0x3e };
            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(data, endHeaders: true, handler: _handler));
            Assert.Equal(SR.net_http_hpack_late_dynamic_table_size_update, exception.Message);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_AfterIndexedHeaderStatic_ResetAfterEndHeaders_Succeeds()
        {
            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            _decoder.Decode(_indexedHeaderStatic, endHeaders: true, handler: _handler);
            Assert.Equal("GET", _handler.DecodedHeaders[":method"]);

            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            _decoder.Decode(new byte[] { 0x3e }, endHeaders: true, handler: _handler);

            Assert.Equal(30, _dynamicTable.MaxSize);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_GreaterThanLimit_Error()
        {
            // 001                     (Dynamic Table Size Update)
            // 11111 11100010 00011111 (4097 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)

            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() =>
                _decoder.Decode(new byte[] { 0x3f, 0xe2, 0x1f }, endHeaders: true, handler: _handler));
            Assert.Equal(SR.Format(SR.net_http_hpack_large_table_size_update, 4097, DynamicTableInitialMaxSize), exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesStringLength_GreaterThanLimit_Error()
        {
            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(new byte[] { 0xff, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix
                .ToArray();

            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(encoded, endHeaders: true, handler: _handler));
            Assert.Equal(SR.Format(SR.net_http_headers_exceeded_length, MaxHeaderFieldSize), exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        [Fact]
        public void DecodesStringLength_LimitConfigurable()
        {
            HPackDecoder decoder = new HPackDecoder(DynamicTableInitialMaxSize, MaxHeaderFieldSize + 1);
            string string8193 = new string('a', MaxHeaderFieldSize + 1);

            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
                .Concat(Encoding.ASCII.GetBytes(string8193))
                .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
                .Concat(Encoding.ASCII.GetBytes(string8193))
                .ToArray();

            decoder.Decode(encoded, endHeaders: true, handler: _handler);

            Assert.Equal(string8193, _handler.DecodedHeaders[string8193]);
        }

        [Fact]
        public void DecodesStringLength_IndividualBytes()
        {
            HPackDecoder decoder = new HPackDecoder(DynamicTableInitialMaxSize, MaxHeaderFieldSize + 1);
            string string8193 = new string('a', MaxHeaderFieldSize + 1);

            byte[] encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
                .Concat(Encoding.ASCII.GetBytes(string8193))
                .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
                .Concat(Encoding.ASCII.GetBytes(string8193))
                .ToArray();

            for (int i = 0; i < encoded.Length; i++)
            {
                bool end = i + 1 == encoded.Length;

                decoder.Decode(new byte[] { encoded[i] }, endHeaders: end, handler: _handler);
            }

            Assert.Equal(string8193, _handler.DecodedHeaders[string8193]);
        }

        [Fact]
        public void DecodesHeaderNameAndValue_SeparateSegments()
        {
            HPackDecoder decoder = new HPackDecoder(DynamicTableInitialMaxSize, MaxHeaderFieldSize + 1);
            string string8193 = new string('a', MaxHeaderFieldSize + 1);

            byte[][] segments = new byte[][]
            {
                _literalHeaderFieldWithoutIndexingNewName,
                new byte[] { 0x7f, 0x82, 0x3f }, // 8193 encoded with 7-bit prefix, no Huffman encoding
                Encoding.ASCII.GetBytes(string8193),
                new byte[] { 0x7f, 0x82, 0x3f }, // 8193 encoded with 7-bit prefix, no Huffman encoding
                Encoding.ASCII.GetBytes(string8193)
            };

            for (int i = 0; i < segments.Length; i++)
            {
                bool end = i + 1 == segments.Length;

                decoder.Decode(segments[i], endHeaders: end, handler: _handler);
            }

            Assert.Equal(string8193, _handler.DecodedHeaders[string8193]);
        }

        public static readonly TheoryData<byte[]> _incompleteHeaderBlockData = new TheoryData<byte[]>
        {
            // Indexed Header Field Representation - incomplete index encoding
            new byte[] { 0xff },

            // Literal Header Field with Incremental Indexing Representation - New Name - incomplete header name length encoding
            new byte[] { 0x40, 0x7f },

            // Literal Header Field with Incremental Indexing Representation - New Name - incomplete header name
            new byte[] { 0x40, 0x01 },
            new byte[] { 0x40, 0x02, 0x61 },

            // Literal Header Field with Incremental Indexing Representation - New Name - incomplete header value length encoding
            new byte[] { 0x40, 0x01, 0x61, 0x7f },

            // Literal Header Field with Incremental Indexing Representation - New Name - incomplete header value
            new byte[] { 0x40, 0x01, 0x61, 0x01 },
            new byte[] { 0x40, 0x01, 0x61, 0x02, 0x61 },

            // Literal Header Field with Incremental Indexing Representation - Indexed Name - incomplete index encoding
            new byte[] { 0x7f },

            // Literal Header Field with Incremental Indexing Representation - Indexed Name - incomplete header value length encoding
            new byte[] { 0x7a, 0xff },

            // Literal Header Field with Incremental Indexing Representation - Indexed Name - incomplete header value
            new byte[] { 0x7a, 0x01 },
            new byte[] { 0x7a, 0x02, 0x61 },

            // Literal Header Field without Indexing - New Name - incomplete header name length encoding
            new byte[] { 0x00, 0xff },

            // Literal Header Field without Indexing - New Name - incomplete header name
            new byte[] { 0x00, 0x01 },
            new byte[] { 0x00, 0x02, 0x61 },

            // Literal Header Field without Indexing - New Name - incomplete header value length encoding
            new byte[] { 0x00, 0x01, 0x61, 0xff },

            // Literal Header Field without Indexing - New Name - incomplete header value
            new byte[] { 0x00, 0x01, 0x61, 0x01 },
            new byte[] { 0x00, 0x01, 0x61, 0x02, 0x61 },

            // Literal Header Field without Indexing Representation - Indexed Name - incomplete index encoding
            new byte[] { 0x0f },

            // Literal Header Field without Indexing Representation - Indexed Name - incomplete header value length encoding
            new byte[] { 0x02, 0xff },

            // Literal Header Field without Indexing Representation - Indexed Name - incomplete header value
            new byte[] { 0x02, 0x01 },
            new byte[] { 0x02, 0x02, 0x61 },

            // Literal Header Field Never Indexed - New Name - incomplete header name length encoding
            new byte[] { 0x10, 0xff },

            // Literal Header Field Never Indexed - New Name - incomplete header name
            new byte[] { 0x10, 0x01 },
            new byte[] { 0x10, 0x02, 0x61 },

            // Literal Header Field Never Indexed - New Name - incomplete header value length encoding
            new byte[] { 0x10, 0x01, 0x61, 0xff },

            // Literal Header Field Never Indexed - New Name - incomplete header value
            new byte[] { 0x10, 0x01, 0x61, 0x01 },
            new byte[] { 0x10, 0x01, 0x61, 0x02, 0x61 },

            // Literal Header Field Never Indexed Representation - Indexed Name - incomplete index encoding
            new byte[] { 0x1f },

            // Literal Header Field Never Indexed Representation - Indexed Name - incomplete header value length encoding
            new byte[] { 0x12, 0xff },

            // Literal Header Field Never Indexed Representation - Indexed Name - incomplete header value
            new byte[] { 0x12, 0x01 },
            new byte[] { 0x12, 0x02, 0x61 },

            // Dynamic Table Size Update - incomplete max size encoding
            new byte[] { 0x3f }
        };

        [Theory]
        [MemberData(nameof(_incompleteHeaderBlockData))]
        public void DecodesIncompleteHeaderBlock_Error(byte[] encoded)
        {
            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(encoded, endHeaders: true, handler: _handler));
            Assert.Equal(SR.net_http_hpack_incomplete_header_block, exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        public static readonly TheoryData<byte[]> _huffmanDecodingErrorData = new TheoryData<byte[]>
        {
            // Invalid Huffman encoding in header name

            _literalHeaderFieldWithIndexingNewName.Concat(_huffmanLongPadding).ToArray(),
            _literalHeaderFieldWithIndexingNewName.Concat(_huffmanEos).ToArray(),

            _literalHeaderFieldWithoutIndexingNewName.Concat(_huffmanLongPadding).ToArray(),
            _literalHeaderFieldWithoutIndexingNewName.Concat(_huffmanEos).ToArray(),

            _literalHeaderFieldNeverIndexedNewName.Concat(_huffmanLongPadding).ToArray(),
            _literalHeaderFieldNeverIndexedNewName.Concat(_huffmanEos).ToArray(),

            // Invalid Huffman encoding in header value

            _literalHeaderFieldWithIndexingIndexedName.Concat(_huffmanLongPadding).ToArray(),
            _literalHeaderFieldWithIndexingIndexedName.Concat(_huffmanEos).ToArray(),

            _literalHeaderFieldWithoutIndexingIndexedName.Concat(_huffmanLongPadding).ToArray(),
            _literalHeaderFieldWithoutIndexingIndexedName.Concat(_huffmanEos).ToArray(),

            _literalHeaderFieldNeverIndexedIndexedName.Concat(_huffmanLongPadding).ToArray(),
            _literalHeaderFieldNeverIndexedIndexedName.Concat(_huffmanEos).ToArray()
        };

        [Theory]
        [MemberData(nameof(_huffmanDecodingErrorData))]
        public void WrapsHuffmanDecodingExceptionInHPackDecodingException(byte[] encoded)
        {
            HPackDecodingException exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(encoded, endHeaders: true, handler: _handler));
            Assert.Equal(SR.net_http_hpack_huffman_decode_failed, exception.Message);
            Assert.IsType<HuffmanDecodingException>(exception.InnerException);
            Assert.Empty(_handler.DecodedHeaders);
        }

        private static void TestDecodeWithIndexing(byte[] encoded, string expectedHeaderName, string expectedHeaderValue)
        {
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: true, byteAtATime: false);
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: true, byteAtATime: true);
        }

        private static void TestDecodeWithoutIndexing(byte[] encoded, string expectedHeaderName, string expectedHeaderValue)
        {
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: false, byteAtATime: false);
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: false, byteAtATime: true);
        }

        private static void TestDecode(byte[] encoded, string expectedHeaderName, string expectedHeaderValue, bool expectDynamicTableEntry, bool byteAtATime)
        {
            var (dynamicTable, decoder) = CreateDecoderAndTable();
            var handler = new TestHttpHeadersHandler();

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);

            if (!byteAtATime)
            {
                decoder.Decode(encoded, endHeaders: true, handler: handler);
            }
            else
            {
                // Parse data in 1 byte chunks, separated by empty chunks
                for (int i = 0; i < encoded.Length; i++)
                {
                    bool end = i + 1 == encoded.Length;

                    decoder.Decode(Array.Empty<byte>(), endHeaders: false, handler: handler);
                    decoder.Decode(new byte[] { encoded[i] }, endHeaders: end, handler: handler);
                }
            }

            Assert.Equal(expectedHeaderValue, handler.DecodedHeaders[expectedHeaderName]);

            if (expectDynamicTableEntry)
            {
                Assert.Equal(1, dynamicTable.Count);
                Assert.Equal(expectedHeaderName, Encoding.ASCII.GetString(dynamicTable[0].Name));
                Assert.Equal(expectedHeaderValue, Encoding.ASCII.GetString(dynamicTable[0].Value));
                Assert.Equal(expectedHeaderName.Length + expectedHeaderValue.Length + 32, dynamicTable.Size);
            }
            else
            {
                Assert.Equal(0, dynamicTable.Count);
                Assert.Equal(0, dynamicTable.Size);
            }
        }
    }

    public class TestHttpHeadersHandler : IHttpHeadersHandler
    {
        public Dictionary<string, string> DecodedHeaders { get; } = new Dictionary<string, string>();
        public Dictionary<int, KeyValuePair<string, string>> DecodedStaticHeaders { get; } = new Dictionary<int, KeyValuePair<string, string>>();

        void IHttpHeadersHandler.OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            string headerName = Encoding.ASCII.GetString(name);
            string headerValue = Encoding.ASCII.GetString(value);

            DecodedHeaders[headerName] = headerValue;
        }

        void IHttpHeadersHandler.OnStaticIndexedHeader(int index)
        {
            ref readonly HeaderField entry = ref H2StaticTable.Get(index - 1);
            ((IHttpHeadersHandler)this).OnHeader(entry.Name, entry.Value);
            DecodedStaticHeaders[index] = new KeyValuePair<string, string>(Encoding.ASCII.GetString(entry.Name), Encoding.ASCII.GetString(entry.Value));
        }

        void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            byte[] name = H2StaticTable.Get(index - 1).Name;
            ((IHttpHeadersHandler)this).OnHeader(name, value);
            DecodedStaticHeaders[index] = new KeyValuePair<string, string>(Encoding.ASCII.GetString(name), Encoding.ASCII.GetString(value));
        }

        void IHttpHeadersHandler.OnHeadersComplete(bool endStream) { }
    }
}
