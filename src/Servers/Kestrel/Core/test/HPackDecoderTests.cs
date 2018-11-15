// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HPackDecoderTests : IHttpHeadersHandler
    {
        private const int DynamicTableInitialMaxSize = 4096;
        private const int MaxRequestHeaderFieldSize = 8192;

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

        private static readonly byte[] _userAgentBytes = Encoding.ASCII.GetBytes(_userAgentString);

        private const string _headerNameString = "new-header";

        private static readonly byte[] _headerNameBytes = Encoding.ASCII.GetBytes(_headerNameString);

        // n     e     w       -      h     e     a     d     e     r      *
        // 10101000 10111110 00010110 10011100 10100011 10010000 10110110 01111111
        private static readonly byte[] _headerNameHuffmanBytes = new byte[] { 0xa8, 0xbe, 0x16, 0x9c, 0xa3, 0x90, 0xb6, 0x7f };

        private const string _headerValueString = "value";

        private static readonly byte[] _headerValueBytes = Encoding.ASCII.GetBytes(_headerValueString);

        // v      a     l      u      e    *
        // 11101110 00111010 00101101 00101111
        private static readonly byte[] _headerValueHuffmanBytes = new byte [] { 0xee, 0x3a, 0x2d, 0x2f };

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

        private readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>();

        public HPackDecoderTests()
        {
            _dynamicTable = new DynamicTable(DynamicTableInitialMaxSize);
            _decoder = new HPackDecoder(DynamicTableInitialMaxSize, MaxRequestHeaderFieldSize, _dynamicTable);
        }

        void IHttpHeadersHandler.OnHeader(Span<byte> name, Span<byte> value)
        {
            _decodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiStringNonNullCharacters();
        }

        [Fact]
        public void DecodesIndexedHeaderField_StaticTable()
        {
            _decoder.Decode(new ReadOnlySequence<byte>(_indexedHeaderStatic), endHeaders: true, handler: this);
            Assert.Equal("GET", _decodedHeaders[HeaderNames.Method]);
        }

        [Fact]
        public void DecodesIndexedHeaderField_DynamicTable()
        {
            // Add the header to the dynamic table
            _dynamicTable.Insert(_headerNameBytes, _headerValueBytes);

            // Index it
            _decoder.Decode(new ReadOnlySequence<byte>(_indexedHeaderDynamic), endHeaders: true, handler: this);
            Assert.Equal(_headerValueString, _decodedHeaders[_headerNameString]);
        }

        [Fact]
        public void DecodesIndexedHeaderField_OutOfRange_Error()
        {
            var exception = Assert.Throws<HPackDecodingException>(() =>
                _decoder.Decode(new ReadOnlySequence<byte>(_indexedHeaderDynamic), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.FormatHPackErrorIndexOutOfRange(62), exception.Message);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName()
        {
            var encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName_HuffmanEncodedName()
        {
            var encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName_HuffmanEncodedValue()
        {
            var encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_NewName_HuffmanEncodedNameAndValue()
        {
            var encoded = _literalHeaderFieldWithIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_IndexedName()
        {
            var encoded = _literalHeaderFieldWithIndexingIndexedName
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithIncrementalIndexing_IndexedName_HuffmanEncodedValue()
        {
            var encoded = _literalHeaderFieldWithIndexingIndexedName
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

            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new ReadOnlySequence<byte>(new byte[] { 0x7e }), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.FormatHPackErrorIndexOutOfRange(62), exception.Message);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName()
        {
            var encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName_HuffmanEncodedName()
        {
            var encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName_HuffmanEncodedValue()
        {
            var encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerName)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_NewName_HuffmanEncodedNameAndValue()
        {
            var encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_IndexedName()
        {
            var encoded = _literalHeaderFieldWithoutIndexingIndexedName
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _userAgentString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldWithoutIndexing_IndexedName_HuffmanEncodedValue()
        {
            var encoded = _literalHeaderFieldWithoutIndexingIndexedName
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

            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new ReadOnlySequence<byte>(new byte[] { 0x0f, 0x2f }), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.FormatHPackErrorIndexOutOfRange(62), exception.Message);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName()
        {
            var encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerName)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_HuffmanEncodedName()
        {
            var encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerNameHuffman)
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_HuffmanEncodedValue()
        {
            var encoded = _literalHeaderFieldNeverIndexedNewName
                .Concat(_headerName)
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralHeaderFieldNeverIndexed_NewName_HuffmanEncodedNameAndValue()
        {
            var encoded = _literalHeaderFieldNeverIndexedNewName
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
            var encoded = _literalHeaderFieldNeverIndexedIndexedName
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
            var encoded = _literalHeaderFieldNeverIndexedIndexedName
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

            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new ReadOnlySequence<byte>(new byte[] { 0x1f, 0x2f }), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.FormatHPackErrorIndexOutOfRange(62), exception.Message);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate()
        {
            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)

            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            _decoder.Decode(new ReadOnlySequence<byte>(new byte[] { 0x3e }), endHeaders: true, handler: this);

            Assert.Equal(30, _dynamicTable.MaxSize);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_AfterIndexedHeaderStatic_Error()
        {
            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)

            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            var data = new ReadOnlySequence<byte>(_indexedHeaderStatic.Concat(new byte[] { 0x3e }).ToArray());
            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(data, endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.HPackErrorDynamicTableSizeUpdateNotAtBeginningOfHeaderBlock, exception.Message);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_AfterIndexedHeaderStatic_SubsequentDecodeCall_Error()
        {
            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            _decoder.Decode(new ReadOnlySequence<byte>(_indexedHeaderStatic), endHeaders: false, handler: this);
            Assert.Equal("GET", _decodedHeaders[HeaderNames.Method]);

            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            var data = new ReadOnlySequence<byte>(new byte[] { 0x3e });
            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(data, endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.HPackErrorDynamicTableSizeUpdateNotAtBeginningOfHeaderBlock, exception.Message);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_AfterIndexedHeaderStatic_ResetAfterEndHeaders_Succeeds()
        {
            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            _decoder.Decode(new ReadOnlySequence<byte>(_indexedHeaderStatic), endHeaders: true, handler: this);
            Assert.Equal("GET", _decodedHeaders[HeaderNames.Method]);

            // 001   (Dynamic Table Size Update)
            // 11110 (30 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)
            _decoder.Decode(new ReadOnlySequence<byte>(new byte[] { 0x3e }), endHeaders: true, handler: this);

            Assert.Equal(30, _dynamicTable.MaxSize);
        }

        [Fact]
        public void DecodesDynamicTableSizeUpdate_GreaterThanLimit_Error()
        {
            // 001                     (Dynamic Table Size Update)
            // 11111 11100010 00011111 (4097 encoded with 5-bit prefix - see http://httpwg.org/specs/rfc7541.html#integer.representation)

            Assert.Equal(DynamicTableInitialMaxSize, _dynamicTable.MaxSize);

            var exception = Assert.Throws<HPackDecodingException>(() =>
                _decoder.Decode(new ReadOnlySequence<byte>(new byte[] { 0x3f, 0xe2, 0x1f }), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.FormatHPackErrorDynamicTableSizeUpdateTooLarge(4097, DynamicTableInitialMaxSize), exception.Message);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesStringLength_GreaterThanLimit_Error()
        {
            var encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(new byte[] { 0xff, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix
                .ToArray();

            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new ReadOnlySequence<byte>(encoded), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.FormatHPackStringLengthTooLarge(MaxRequestHeaderFieldSize + 1, MaxRequestHeaderFieldSize), exception.Message);
            Assert.Empty(_decodedHeaders);
        }

        [Fact]
        public void DecodesStringLength_LimitConfigurable()
        {
            var decoder = new HPackDecoder(DynamicTableInitialMaxSize, MaxRequestHeaderFieldSize + 1);
            var string8193 = new string('a', MaxRequestHeaderFieldSize + 1);

            var encoded = _literalHeaderFieldWithoutIndexingNewName
                .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
                .Concat(Encoding.ASCII.GetBytes(string8193))
                .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
                .Concat(Encoding.ASCII.GetBytes(string8193))
                .ToArray();

            decoder.Decode(new ReadOnlySequence<byte>(encoded), endHeaders: true, handler: this);

            Assert.Equal(string8193, _decodedHeaders[string8193]);
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
            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new ReadOnlySequence<byte>(encoded), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.HPackErrorIncompleteHeaderBlock, exception.Message);
            Assert.Empty(_decodedHeaders);
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
            var exception = Assert.Throws<HPackDecodingException>(() => _decoder.Decode(new ReadOnlySequence<byte>(encoded), endHeaders: true, handler: this));
            Assert.Equal(CoreStrings.HPackHuffmanError, exception.Message);
            Assert.IsType<HuffmanDecodingException>(exception.InnerException);
            Assert.Empty(_decodedHeaders);
        }

        private void TestDecodeWithIndexing(byte[] encoded, string expectedHeaderName, string expectedHeaderValue)
        {
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: true);
        }

        private void TestDecodeWithoutIndexing(byte[] encoded, string expectedHeaderName, string expectedHeaderValue)
        {
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: false);
        }

        private void TestDecode(byte[] encoded, string expectedHeaderName, string expectedHeaderValue, bool expectDynamicTableEntry)
        {
            Assert.Equal(0, _dynamicTable.Count);
            Assert.Equal(0, _dynamicTable.Size);

            _decoder.Decode(new ReadOnlySequence<byte>(encoded), endHeaders: true, handler: this);

            Assert.Equal(expectedHeaderValue, _decodedHeaders[expectedHeaderName]);

            if (expectDynamicTableEntry)
            {
                Assert.Equal(1, _dynamicTable.Count);
                Assert.Equal(expectedHeaderName, Encoding.ASCII.GetString(_dynamicTable[0].Name));
                Assert.Equal(expectedHeaderValue, Encoding.ASCII.GetString(_dynamicTable[0].Value));
                Assert.Equal(expectedHeaderName.Length + expectedHeaderValue.Length + 32, _dynamicTable.Size);
            }
            else
            {
                Assert.Equal(0, _dynamicTable.Count);
                Assert.Equal(0, _dynamicTable.Size);
            }
        }
    }
}
