// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.HPack;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2HPackEncoderTests
{
    [Fact]
    public void BeginEncodeHeaders_Status302_NewIndexValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = new HttpResponseHeaders();
        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(302, hpackEncoder, enumerator, buffer, out var length));

        var result = buffer.Slice(0, length).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("48-03-33-30-32", hex);

        var statusHeader = GetHeaderEntry(hpackEncoder, 0);
        Assert.Equal(":status", statusHeader.Name);
        Assert.Equal("302", statusHeader.Value);
    }

    [Fact]
    public void BeginEncodeHeaders_CacheControlPrivate_NewIndexValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.CacheControl = "private";

        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(302, hpackEncoder, enumerator, buffer, out var length));

        var result = buffer.Slice(5, length - 5).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("58-07-70-72-69-76-61-74-65", hex);

        var statusHeader = GetHeaderEntry(hpackEncoder, 0);
        Assert.Equal("Cache-Control", statusHeader.Name);
        Assert.Equal("private", statusHeader.Value);
    }

    [Fact]
    public void BeginEncodeHeaders_MaxHeaderTableSizeExceeded_EvictionsToFit()
    {
        // Test follows example https://tools.ietf.org/html/rfc7541#appendix-C.5
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.CacheControl = "private";
        headers.Date = "Mon, 21 Oct 2013 20:13:21 GMT";
        headers.Location = "https://www.example.com";

        var enumerator = new Http2HeadersEnumerator();

        var hpackEncoder = new DynamicHPackEncoder(maxHeaderTableSize: 256);

        // First response
        enumerator.Initialize(headers);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(302, hpackEncoder, enumerator, buffer, out var length));

        var result = buffer.Slice(0, length).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal(
            "48-03-33-30-32-61-1D-4D-6F-6E-2C-20-32-31-20-4F-" +
            "63-74-20-32-30-31-33-20-32-30-3A-31-33-3A-32-31-" +
            "20-47-4D-54-58-07-70-72-69-76-61-74-65-6E-17-68-" +
            "74-74-70-73-3A-2F-2F-77-77-77-2E-65-78-61-6D-70-" +
            "6C-65-2E-63-6F-6D", hex);

        var entries = GetHeaderEntries(hpackEncoder);
        Assert.Collection(entries,
            e =>
            {
                Assert.Equal("Location", e.Name);
                Assert.Equal("https://www.example.com", e.Value);
                Assert.Equal(63u, e.Size);
            },
            e =>
            {
                Assert.Equal("Cache-Control", e.Name);
                Assert.Equal("private", e.Value);
                Assert.Equal(52u, e.Size);
            },
            e =>
            {
                Assert.Equal("Date", e.Name);
                Assert.Equal("Mon, 21 Oct 2013 20:13:21 GMT", e.Value);
                Assert.Equal(65u, e.Size);
            },
            e =>
            {
                Assert.Equal(":status", e.Name);
                Assert.Equal("302", e.Value);
                Assert.Equal(42u, e.Size);
            });

        Assert.Equal(222u, hpackEncoder.TableSize);

        // Second response
        enumerator.Initialize(headers);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(307, hpackEncoder, enumerator, buffer, out length));

        result = buffer.Slice(0, length).ToArray();
        hex = BitConverter.ToString(result);
        Assert.Equal("48-03-33-30-37-C1-C0-BF", hex);

        entries = GetHeaderEntries(hpackEncoder);
        Assert.Collection(entries,
            e =>
            {
                Assert.Equal(":status", e.Name);
                Assert.Equal("307", e.Value);
                Assert.Equal(42u, e.Size);
            },
            e =>
            {
                Assert.Equal("Location", e.Name);
                Assert.Equal("https://www.example.com", e.Value);
                Assert.Equal(63u, e.Size);
            },
            e =>
            {
                Assert.Equal("Cache-Control", e.Name);
                Assert.Equal("private", e.Value);
                Assert.Equal(52u, e.Size);
            },
            e =>
            {
                Assert.Equal("Date", e.Name);
                Assert.Equal("Mon, 21 Oct 2013 20:13:21 GMT", e.Value);
                Assert.Equal(65u, e.Size);
            });

        Assert.Equal(222u, hpackEncoder.TableSize);

        // Third response
        headers.Date = "Mon, 21 Oct 2013 20:13:22 GMT";
        headers.ContentEncoding = "gzip";
        headers.SetCookie = "foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1";

        enumerator.Initialize(headers);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(200, hpackEncoder, enumerator, buffer, out length));

        result = buffer.Slice(0, length).ToArray();
        hex = BitConverter.ToString(result);
        Assert.Equal(
            "88-61-1D-4D-6F-6E-2C-20-32-31-20-4F-63-74-20-32-" +
            "30-31-33-20-32-30-3A-31-33-3A-32-32-20-47-4D-54-" +
            "C1-5A-04-67-7A-69-70-C1-1F-28-38-66-6F-6F-3D-41-" +
            "53-44-4A-4B-48-51-4B-42-5A-58-4F-51-57-45-4F-50-" +
            "49-55-41-58-51-57-45-4F-49-55-3B-20-6D-61-78-2D-" +
            "61-67-65-3D-33-36-30-30-3B-20-76-65-72-73-69-6F-" +
            "6E-3D-31", hex);

        entries = GetHeaderEntries(hpackEncoder);
        Assert.Collection(entries,
            e =>
            {
                Assert.Equal("Content-Encoding", e.Name);
                Assert.Equal("gzip", e.Value);
                Assert.Equal(52u, e.Size);
            },
            e =>
            {
                Assert.Equal("Date", e.Name);
                Assert.Equal("Mon, 21 Oct 2013 20:13:22 GMT", e.Value);
                Assert.Equal(65u, e.Size);
            },
            e =>
            {
                Assert.Equal(":status", e.Name);
                Assert.Equal("307", e.Value);
                Assert.Equal(42u, e.Size);
            },
            e =>
            {
                Assert.Equal("Location", e.Name);
                Assert.Equal("https://www.example.com", e.Value);
                Assert.Equal(63u, e.Size);
            });

        Assert.Equal(222u, hpackEncoder.TableSize);
    }

    [Fact]
    public void BeginEncodeHeadersCustomEncoding_MaxHeaderTableSizeExceeded_EvictionsToFit()
    {
        // Test follows example https://tools.ietf.org/html/rfc7541#appendix-C.5

        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders(_ => Encoding.UTF8);
        headers.CacheControl = "你好e";
        headers.Date = "Mon, 21 Oct 2013 20:13:21 GMT";
        headers.Location = "你好你好你好你.c";

        var enumerator = new Http2HeadersEnumerator();

        var hpackEncoder = new DynamicHPackEncoder(maxHeaderTableSize: 256);

        // First response
        enumerator.Initialize((HttpResponseHeaders)headers);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(302, hpackEncoder, enumerator, buffer, out var length));

        var result = buffer.Slice(0, length).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal(
            "48-03-33-30-32-61-1D-4D-6F-6E-2C-20-32-31-20-4F-" +
            "63-74-20-32-30-31-33-20-32-30-3A-31-33-3A-32-31-" +
            "20-47-4D-54-58-07-E4-BD-A0-E5-A5-BD-65-6E-17-E4-" +
            "BD-A0-E5-A5-BD-E4-BD-A0-E5-A5-BD-E4-BD-A0-E5-A5-" +
            "BD-E4-BD-A0-2E-63", hex);

        var entries = GetHeaderEntries(hpackEncoder);
        Assert.Collection(entries,
            e =>
            {
                Assert.Equal("Location", e.Name);
                Assert.Equal("你好你好你好你.c", e.Value);
                Assert.Equal(63u, e.Size);
            },
            e =>
            {
                Assert.Equal("Cache-Control", e.Name);
                Assert.Equal("你好e", e.Value);
                Assert.Equal(52u, e.Size);
            },
            e =>
            {
                Assert.Equal("Date", e.Name);
                Assert.Equal("Mon, 21 Oct 2013 20:13:21 GMT", e.Value);
                Assert.Equal(65u, e.Size);
            },
            e =>
            {
                Assert.Equal(":status", e.Name);
                Assert.Equal("302", e.Value);
                Assert.Equal(42u, e.Size);
            });

        Assert.Equal(222u, hpackEncoder.TableSize);

        // Second response
        enumerator.Initialize(headers);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(307, hpackEncoder, enumerator, buffer, out length));

        result = buffer.Slice(0, length).ToArray();
        hex = BitConverter.ToString(result);
        Assert.Equal("48-03-33-30-37-C1-C0-BF", hex);

        entries = GetHeaderEntries(hpackEncoder);
        Assert.Collection(entries,
            e =>
            {
                Assert.Equal(":status", e.Name);
                Assert.Equal("307", e.Value);
                Assert.Equal(42u, e.Size);
            },
            e =>
            {
                Assert.Equal("Location", e.Name);
                Assert.Equal("你好你好你好你.c", e.Value);
                Assert.Equal(63u, e.Size);
            },
            e =>
            {
                Assert.Equal("Cache-Control", e.Name);
                Assert.Equal("你好e", e.Value);
                Assert.Equal(52u, e.Size);
            },
            e =>
            {
                Assert.Equal("Date", e.Name);
                Assert.Equal("Mon, 21 Oct 2013 20:13:21 GMT", e.Value);
                Assert.Equal(65u, e.Size);
            });

        Assert.Equal(222u, hpackEncoder.TableSize);

        // Third response
        headers.Date = "Mon, 21 Oct 2013 20:13:22 GMT";
        headers.ContentEncoding = "gzip";
        headers.SetCookie = "foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1";

        enumerator.Initialize(headers);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(200, hpackEncoder, enumerator, buffer, out length));

        result = buffer.Slice(0, length).ToArray();
        hex = BitConverter.ToString(result);
        Assert.Equal(
            "88-61-1D-4D-6F-6E-2C-20-32-31-20-4F-63-74-20-32-" +
            "30-31-33-20-32-30-3A-31-33-3A-32-32-20-47-4D-54-" +
            "C1-5A-04-67-7A-69-70-C1-1F-28-38-66-6F-6F-3D-41-" +
            "53-44-4A-4B-48-51-4B-42-5A-58-4F-51-57-45-4F-50-" +
            "49-55-41-58-51-57-45-4F-49-55-3B-20-6D-61-78-2D-" +
            "61-67-65-3D-33-36-30-30-3B-20-76-65-72-73-69-6F-" +
            "6E-3D-31", hex);

        entries = GetHeaderEntries(hpackEncoder);
        Assert.Collection(entries,
            e =>
            {
                Assert.Equal("Content-Encoding", e.Name);
                Assert.Equal("gzip", e.Value);
                Assert.Equal(52u, e.Size);
            },
            e =>
            {
                Assert.Equal("Date", e.Name);
                Assert.Equal("Mon, 21 Oct 2013 20:13:22 GMT", e.Value);
                Assert.Equal(65u, e.Size);
            },
            e =>
            {
                Assert.Equal(":status", e.Name);
                Assert.Equal("307", e.Value);
                Assert.Equal(42u, e.Size);
            },
            e =>
            {
                Assert.Equal("Location", e.Name);
                Assert.Equal("你好你好你好你.c", e.Value);
                Assert.Equal(63u, e.Size);
            });

        Assert.Equal(222u, hpackEncoder.TableSize);
    }

    [Theory]
    [InlineData("Set-Cookie", true)]
    [InlineData("Content-Disposition", true)]
    [InlineData("Content-Length", false)]
    public void BeginEncodeHeaders_ExcludedHeaders_NotAddedToTable(string headerName, bool neverIndex)
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = new HttpResponseHeaders();
        headers.Append(headerName, "1");

        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder(maxHeaderTableSize: Http2PeerSettings.DefaultHeaderTableSize);
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, enumerator, buffer, out _));

        if (neverIndex)
        {
            Assert.Equal(0x10, buffer[0] & 0x10);
        }
        else
        {
            Assert.Equal(0, buffer[0] & 0x40);
        }

        Assert.Empty(GetHeaderEntries(hpackEncoder));
    }

    [Fact]
    public void BeginEncodeHeaders_HeaderExceedHeaderTableSize_NoIndexAndNoHeaderEntry()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = new HttpResponseHeaders();
        headers.Append("x-Custom", new string('!', (int)Http2PeerSettings.DefaultHeaderTableSize));

        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(200, hpackEncoder, enumerator, buffer, out var length));

        Assert.Empty(GetHeaderEntries(hpackEncoder));
    }

    public static TheoryData<KeyValuePair<string, string>[], byte[], int?> SinglePayloadData
    {
        get
        {
            var data = new TheoryData<KeyValuePair<string, string>[], byte[], int?>();

            // Lowercase header name letters only
            data.Add(
                new[]
                {
                        new KeyValuePair<string, string>("CustomHeader", "CustomValue"),
                },
                new byte[]
                {
                        //      12     c     u     s     t     o     m
                        0x40, 0x0c, 0x63, 0x75, 0x73, 0x74, 0x6f, 0x6d,
                        // h     e     a     d     e     r    11     C
                        0x68, 0x65, 0x61, 0x64, 0x65, 0x72, 0x0b, 0x43,
                        // u     s     t     o     m     V     a     l
                        0x75, 0x73, 0x74, 0x6f, 0x6d, 0x56, 0x61, 0x6c,
                        // u     e
                        0x75, 0x65
                },
                null);
            // Lowercase header name letters only
            data.Add(
                new[]
                {
                        new KeyValuePair<string, string>("CustomHeader!#$%&'*+-.^_`|~", "CustomValue"),
                },
                new byte[]
                {
                        //      27     c     u     s     t     o     m
                        0x40, 0x1b, 0x63, 0x75, 0x73, 0x74, 0x6f, 0x6d,
                        // h     e     a     d     e     r     !     #
                        0x68, 0x65, 0x61, 0x64, 0x65, 0x72, 0x21, 0x23,
                        // $     %     &     '     *     +     -     .
                        0x24, 0x25, 0x26, 0x27, 0x2a, 0x2b, 0x2d, 0x2e,
                        // ^     _     `     |     ~    11     C     u
                        0x5e, 0x5f, 0x60, 0x7c, 0x7e, 0x0b, 0x43, 0x75,
                        // s     t     o     m     V     a     l     u
                        0x73, 0x74, 0x6f, 0x6d, 0x56, 0x61, 0x6c, 0x75,
                        // e
                        0x65
                },
                null);
            // Single Payload
            data.Add(
                new[]
                {
                        new KeyValuePair<string, string>("date", "Mon, 24 Jul 2017 19:22:30 GMT"),
                        new KeyValuePair<string, string>("content-type", "text/html; charset=utf-8"),
                        new KeyValuePair<string, string>("server", "Kestrel")
                },
                new byte[]
                {
                        0x88, 0x40, 0x04, 0x64, 0x61, 0x74, 0x65, 0x1d,
                        0x4d, 0x6f, 0x6e, 0x2c, 0x20, 0x32, 0x34, 0x20,
                        0x4a, 0x75, 0x6c, 0x20, 0x32, 0x30, 0x31, 0x37,
                        0x20, 0x31, 0x39, 0x3a, 0x32, 0x32, 0x3a, 0x33,
                        0x30, 0x20, 0x47, 0x4d, 0x54, 0x40, 0x0c, 0x63,
                        0x6f, 0x6e, 0x74, 0x65, 0x6e, 0x74, 0x2d, 0x74,
                        0x79, 0x70, 0x65, 0x18, 0x74, 0x65, 0x78, 0x74,
                        0x2f, 0x68, 0x74, 0x6d, 0x6c, 0x3b, 0x20, 0x63,
                        0x68, 0x61, 0x72, 0x73, 0x65, 0x74, 0x3d, 0x75,
                        0x74, 0x66, 0x2d, 0x38, 0x40, 0x06, 0x73, 0x65,
                        0x72, 0x76, 0x65, 0x72, 0x07, 0x4b, 0x65, 0x73,
                        0x74, 0x72, 0x65, 0x6c
                },
                200);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SinglePayloadData))]
    public void EncodesHeadersInSinglePayloadWhenSpaceAvailable(KeyValuePair<string, string>[] headers, byte[] expectedPayload, int? statusCode)
    {
        var hpackEncoder = new DynamicHPackEncoder();
        var payload = new byte[1024];
        var length = 0;
        if (statusCode.HasValue)
        {
            Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(statusCode.Value, hpackEncoder, GetHeadersEnumerator(headers), payload, out length));
        }
        else
        {
            Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, GetHeadersEnumerator(headers), payload, out length));
        }
        Assert.Equal(expectedPayload.Length, length);

        for (var i = 0; i < length; i++)
        {
            Assert.True(expectedPayload[i] == payload[i], $"{expectedPayload[i]} != {payload[i]} at {i} (len {length})");
        }

        Assert.Equal(expectedPayload, new ArraySegment<byte>(payload, 0, length).ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EncodesHeadersInMultiplePayloadsWhenSpaceNotAvailable(bool exactSize)
    {
        var statusCode = 200;
        var headers = new[]
        {
                new KeyValuePair<string, string>("date", "Mon, 24 Jul 2017 19:22:30 GMT"),
                new KeyValuePair<string, string>("content-type", "text/html; charset=utf-8"),
                new KeyValuePair<string, string>("server", "Kestrel")
            };

        var expectedStatusCodePayload = new byte[]
        {
                0x88
        };

        var expectedDateHeaderPayload = new byte[]
        {
                0x40, 0x04, 0x64, 0x61, 0x74, 0x65, 0x1d, 0x4d,
                0x6f, 0x6e, 0x2c, 0x20, 0x32, 0x34, 0x20, 0x4a,
                0x75, 0x6c, 0x20, 0x32, 0x30, 0x31, 0x37, 0x20,
                0x31, 0x39, 0x3a, 0x32, 0x32, 0x3a, 0x33, 0x30,
                0x20, 0x47, 0x4d, 0x54
        };

        var expectedContentTypeHeaderPayload = new byte[]
        {
                0x40, 0x0c, 0x63, 0x6f, 0x6e, 0x74, 0x65, 0x6e,
                0x74, 0x2d, 0x74, 0x79, 0x70, 0x65, 0x18, 0x74,
                0x65, 0x78, 0x74, 0x2f, 0x68, 0x74, 0x6d, 0x6c,
                0x3b, 0x20, 0x63, 0x68, 0x61, 0x72, 0x73, 0x65,
                0x74, 0x3d, 0x75, 0x74, 0x66, 0x2d, 0x38
        };

        var expectedServerHeaderPayload = new byte[]
        {
                0x40, 0x06, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72,
                0x07, 0x4b, 0x65, 0x73, 0x74, 0x72, 0x65, 0x6c
        };

        var hpackEncoder = new DynamicHPackEncoder();

        Span<byte> payload = new byte[1024];
        var offset = 0;
        var headerEnumerator = GetHeadersEnumerator(headers);

        // When !exactSize, slices are one byte short of fitting the next header
        var sliceLength = expectedStatusCodePayload.Length + (exactSize ? 0 : expectedDateHeaderPayload.Length - 1);
        Assert.Equal(HeaderWriteResult.MoreHeaders, HPackHeaderWriter.BeginEncodeHeaders(statusCode, hpackEncoder, headerEnumerator, payload.Slice(offset, sliceLength), out var length));
        Assert.Equal(expectedStatusCodePayload.Length, length);
        Assert.Equal(expectedStatusCodePayload, payload.Slice(0, length).ToArray());

        offset += length;

        sliceLength = expectedDateHeaderPayload.Length + (exactSize ? 0 : expectedContentTypeHeaderPayload.Length - 1);
        Assert.Equal(HeaderWriteResult.MoreHeaders, HPackHeaderWriter.ContinueEncodeHeaders(hpackEncoder, headerEnumerator, payload.Slice(offset, sliceLength), out length));
        Assert.Equal(expectedDateHeaderPayload.Length, length);
        Assert.Equal(expectedDateHeaderPayload, payload.Slice(offset, length).ToArray());

        offset += length;

        sliceLength = expectedContentTypeHeaderPayload.Length + (exactSize ? 0 : expectedServerHeaderPayload.Length - 1);
        Assert.Equal(HeaderWriteResult.MoreHeaders, HPackHeaderWriter.ContinueEncodeHeaders(hpackEncoder, headerEnumerator, payload.Slice(offset, sliceLength), out length));
        Assert.Equal(expectedContentTypeHeaderPayload.Length, length);
        Assert.Equal(expectedContentTypeHeaderPayload, payload.Slice(offset, length).ToArray());

        offset += length;

        sliceLength = expectedServerHeaderPayload.Length;
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.ContinueEncodeHeaders(hpackEncoder, headerEnumerator, payload.Slice(offset, sliceLength), out length));
        Assert.Equal(expectedServerHeaderPayload.Length, length);
        Assert.Equal(expectedServerHeaderPayload, payload.Slice(offset, length).ToArray());
    }

    [Fact]
    public void BeginEncodeHeaders_MaxHeaderTableSizeUpdated_SizeUpdateInHeaders()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var hpackEncoder = new DynamicHPackEncoder();
        hpackEncoder.UpdateMaxHeaderTableSize(100);

        var enumerator = new Http2HeadersEnumerator();

        // First request
        enumerator.Initialize(new Dictionary<string, StringValues>());
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, enumerator, buffer, out var length));

        Assert.Equal(2, length);

        const byte DynamicTableSizeUpdateMask = 0xe0;

        var integerDecoder = new IntegerDecoder();
        Assert.False(integerDecoder.BeginTryDecode((byte)(buffer[0] & ~DynamicTableSizeUpdateMask), prefixLength: 5, out _));
        Assert.True(integerDecoder.TryDecode(buffer[1], out var result));

        Assert.Equal(100, result);

        // Second request
        enumerator.Initialize(new Dictionary<string, StringValues>());
        Assert.Equal(HeaderWriteResult.Done, HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, enumerator, buffer, out length));

        Assert.Equal(0, length);
    }

    [Fact]
    public void WithStatusCode_TooLargeHeader_ReturnsMoreHeaders()
    {
        Span<byte> buffer = new byte[1024 * 16];

        IHeaderDictionary headers = new HttpResponseHeaders();
        headers.Cookie = new string('a', buffer.Length + 1);
        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.MoreHeaders, HPackHeaderWriter.BeginEncodeHeaders(200, hpackEncoder, enumerator, buffer, out var length));
        Assert.Equal(1, length);
    }

    [Fact]
    public void NoStatusCodeLargeHeader_ReturnsOversized()
    {
        Span<byte> buffer = new byte[1024 * 16];

        IHeaderDictionary headers = new HttpResponseHeaders();
        headers.Cookie = new string('a', buffer.Length + 1);
        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.BufferTooSmall, HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, enumerator, buffer, out var length));
        Assert.Equal(0, length);
    }

    [Fact]
    public void WithStatusCode_JustFittingHeaderNoSpace_ReturnsMoreHeaders()
    {
        Span<byte> buffer = new byte[1024 * 16];

        IHeaderDictionary headers = new HttpResponseHeaders();
        headers.Cookie = new string('a', buffer.Length - 1);
        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.MoreHeaders, HPackHeaderWriter.BeginEncodeHeaders(200, hpackEncoder, enumerator, buffer, out var length));
        Assert.Equal(1, length);
    }

    [Fact]
    public void NoStatusCode_JustFittingHeaderNoSpace_ReturnsMoreHeaders()
    {
        Span<byte> buffer = new byte[1024 * 16];

        IHeaderDictionary headers = new HttpResponseHeaders();
        headers.Accept = "application/json;";
        headers.Cookie = new string('a', buffer.Length - 1);
        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(headers);

        var hpackEncoder = new DynamicHPackEncoder();
        Assert.Equal(HeaderWriteResult.MoreHeaders, HPackHeaderWriter.BeginEncodeHeaders(hpackEncoder, enumerator, buffer, out var length));
        Assert.Equal(26, length);
    }

    private static Http2HeadersEnumerator GetHeadersEnumerator(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var groupedHeaders = headers
            .GroupBy(k => k.Key)
            .ToDictionary(g => g.Key, g => new StringValues(g.Select(gg => gg.Value).ToArray()));

        var enumerator = new Http2HeadersEnumerator();
        enumerator.Initialize(groupedHeaders);
        return enumerator;
    }

    private EncoderHeaderEntry GetHeaderEntry(DynamicHPackEncoder encoder, int index)
    {
        var entry = encoder.Head;
        while (index-- >= 0)
        {
            entry = entry.Before;
        }
        return entry;
    }

    private List<EncoderHeaderEntry> GetHeaderEntries(DynamicHPackEncoder encoder)
    {
        var headers = new List<EncoderHeaderEntry>();

        var entry = encoder.Head;
        while (entry.Before != encoder.Head)
        {
            entry = entry.Before;
            headers.Add(entry);
        };

        return headers;
    }
}
