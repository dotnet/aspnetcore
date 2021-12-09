// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.Http.QPack;
using System.Net.Http.HPack;
using HeaderField = System.Net.Http.QPack.HeaderField;
#if KESTREL
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
#endif

namespace System.Net.Http.Unit.Tests.QPack
{
    public class QPackDecoderTests
    {
        private const int MaxHeaderFieldSize = 8192;

        // 4.5.2 - Indexed Field Line - Static Table - Index 25 (:method: GET)
        private static readonly byte[] _indexedFieldLineStatic = new byte[] { 0xd1 };

        // 4.5.4 - Literal Header Field With Name Reference - Static Table - Index 44 (content-type)
        private static readonly byte[] _literalHeaderFieldWithNameReferenceStatic = new byte[] { 0x5f, 0x1d };

        // 4.5.6 - Literal Field Line With Literal Name - (translate)
        private static readonly byte[] _literalFieldLineWithLiteralName = new byte[] { 0x37, 0x02, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x6c, 0x61, 0x74, 0x65 };

        private const string _contentTypeString = "content-type";
        private const string _translateString = "translate";

        // n     e     w       -      h     e     a     d     e     r      *
        // 10101000 10111110 00010110 10011100 10100011 10010000 10110110 01111111
        private static readonly byte[] _headerNameHuffmanBytes = new byte[] { 0xa8, 0xbe, 0x16, 0x9c, 0xa3, 0x90, 0xb6, 0x7f };

        private const string _headerNameString = "new-header";
        private const string _headerValueString = "value";

        private static readonly byte[] _headerValueBytes = Encoding.ASCII.GetBytes(_headerValueString);

        // v      a     l      u      e    *
        // 11101110 00111010 00101101 00101111
        private static readonly byte[] _headerValueHuffmanBytes = new byte[] { 0xee, 0x3a, 0x2d, 0x2f };

        private static readonly byte[] _headerNameHuffman = new byte[] { 0x3f, 0x01 }
            .Concat(_headerNameHuffmanBytes)
            .ToArray();

        private static readonly byte[] _headerValue = new byte[] { (byte)_headerValueBytes.Length }
            .Concat(_headerValueBytes)
            .ToArray();

        private static readonly byte[] _headerValueHuffman = new byte[] { (byte)(0x80 | _headerValueHuffmanBytes.Length) }
            .Concat(_headerValueHuffmanBytes)
            .ToArray();

        private readonly QPackDecoder _decoder;
        private readonly TestHttpHeadersHandler _handler = new TestHttpHeadersHandler();

        public QPackDecoderTests()
        {
            _decoder = new QPackDecoder(MaxHeaderFieldSize);
        }

        [Fact]
        public void DecodesIndexedHeaderField_StaticTableWithValue()
        {
            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(_indexedFieldLineStatic, endHeaders: true, handler: _handler);
            Assert.Equal("GET", _handler.DecodedHeaders[":method"]);

            Assert.Equal(":method", _handler.DecodedStaticHeaders[H3StaticTable.MethodGet].Key);
            Assert.Equal("GET", _handler.DecodedStaticHeaders[H3StaticTable.MethodGet].Value);
        }

        [Fact]
        public void DecodesIndexedHeaderField_StaticTableLiteralValue()
        {
            byte[] encoded = _literalHeaderFieldWithNameReferenceStatic
                .Concat(_headerValue)
                .ToArray();

            _decoder.Decode(new byte[] { 0, 0 }, endHeaders: false, handler: _handler);
            _decoder.Decode(encoded, endHeaders: true, handler: _handler);
            Assert.Equal(_headerValueString, _handler.DecodedHeaders[_contentTypeString]);

            Assert.Equal(_contentTypeString, _handler.DecodedStaticHeaders[H3StaticTable.ContentTypeApplicationDnsMessage].Key);
            Assert.Equal(_headerValueString, _handler.DecodedStaticHeaders[H3StaticTable.ContentTypeApplicationDnsMessage].Value);
        }

        [Fact]
        public void DecodesLiteralFieldLineWithLiteralName_Value()
        {
            byte[] encoded = _literalFieldLineWithLiteralName
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _translateString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralFieldLineWithLiteralName_HuffmanEncodedValue()
        {
            byte[] encoded = _literalFieldLineWithLiteralName
                .Concat(_headerValueHuffman)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _translateString, _headerValueString);
        }

        [Fact]
        public void DecodesLiteralFieldLineWithLiteralName_HuffmanEncodedName()
        {
            byte[] encoded = _headerNameHuffman
                .Concat(_headerValue)
                .ToArray();

            TestDecodeWithoutIndexing(encoded, _headerNameString, _headerValueString);
        }

        public static readonly TheoryData<byte[]> _incompleteHeaderBlockData = new TheoryData<byte[]>
        {
            // Incomplete header
            new byte[] { },
            new byte[] { 0x00 },

            // 4.5.4 - Literal Header Field With Name Reference - Static Table - Index 44 (content-type)
            new byte[] { 0x00, 0x00, 0x5f },

            // 4.5.6 - Literal Field Line With Literal Name - (translate)
            new byte[] { 0x00, 0x00, 0x37 },
            new byte[] { 0x00, 0x00, 0x37, 0x02 },
            new byte[] { 0x00, 0x00, 0x37, 0x02, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x6c, 0x61, 0x74 },
        };

        [Theory]
        [MemberData(nameof(_incompleteHeaderBlockData))]
        public void DecodesIncompleteHeaderBlock_Error(byte[] encoded)
        {
            QPackDecodingException exception = Assert.Throws<QPackDecodingException>(() => _decoder.Decode(encoded, endHeaders: true, handler: _handler));
            Assert.Equal(SR.net_http_hpack_incomplete_header_block, exception.Message);
            Assert.Empty(_handler.DecodedHeaders);
        }

        private static void TestDecodeWithoutIndexing(byte[] encoded, string expectedHeaderName, string expectedHeaderValue)
        {
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: false, byteAtATime: false);
            TestDecode(encoded, expectedHeaderName, expectedHeaderValue, expectDynamicTableEntry: false, byteAtATime: true);
        }

        private static void TestDecode(byte[] encoded, string expectedHeaderName, string expectedHeaderValue, bool expectDynamicTableEntry, bool byteAtATime)
        {
            var decoder = new QPackDecoder(MaxHeaderFieldSize);
            var handler = new TestHttpHeadersHandler();

            // Read past header
            decoder.Decode(new byte[] { 0x00, 0x00 }, endHeaders: false, handler: handler);

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
            ref readonly HeaderField entry = ref H3StaticTable.Get(index);
            ((IHttpHeadersHandler)this).OnHeader(entry.Name, entry.Value);
            DecodedStaticHeaders[index] = new KeyValuePair<string, string>(Encoding.ASCII.GetString(entry.Name), Encoding.ASCII.GetString(entry.Value));
        }

        void IHttpHeadersHandler.OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            byte[] name = H3StaticTable.Get(index).Name;
            ((IHttpHeadersHandler)this).OnHeader(name, value);
            DecodedStaticHeaders[index] = new KeyValuePair<string, string>(Encoding.ASCII.GetString(name), Encoding.ASCII.GetString(value));
        }

        void IHttpHeadersHandler.OnHeadersComplete(bool endStream) { }
    }
}
