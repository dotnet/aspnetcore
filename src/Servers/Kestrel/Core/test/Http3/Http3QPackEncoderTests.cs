// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http.QPack;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3QPackEncoderTests
{
    [Fact]
    public void BeginEncodeHeaders_StatusWithoutIndexedValue_WriteIndexNameAndFullValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var totalHeaderSize = 0;
        var headers = new HttpResponseHeaders();
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(418, enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(0, length).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("00-00-5F-30-03-34-31-38", hex);
    }

    [Fact]
    public void BeginEncodeHeaders_StatusWithIndexedValue_WriteIndex()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var totalHeaderSize = 0;
        var headers = new HttpResponseHeaders();
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(200, enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(0, length).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("00-00-D9", hex);
    }

    [Theory]
    [InlineData(103)]
    [InlineData(200)]
    [InlineData(304)]
    [InlineData(404)]
    [InlineData(503)]
    [InlineData(100)]
    [InlineData(204)]
    [InlineData(206)]
    [InlineData(302)]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(421)]
    [InlineData(425)]
    [InlineData(500)]
    public void BeginEncodeHeaders_StatusWithIndexedValue_ExpectedLength(int statusCode)
    {
        Span<byte> buffer = new byte[1024 * 16];

        var totalHeaderSize = 0;
        var headers = new HttpResponseHeaders();
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(statusCode, enumerator, buffer, ref totalHeaderSize, out var length));
        length -= 2; // Remove prefix

        Assert.True(length <= 2, "Indexed header should be encoded into 1 or 2 bytes");
    }

    [Fact]
    public void BeginEncodeHeaders_StaticKeyAndValue_WriteIndex()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.ContentType = "application/json";

        var totalHeaderSize = 0;
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(2, length - 2).ToArray(); // trim prefix
        var hex = BitConverter.ToString(result);
        Assert.Equal("EE", hex);
    }

    [Fact]
    public void BeginEncodeHeaders_NonStaticKey_WriteFullNameAndFullValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.Translate = "private";

        var totalHeaderSize = 0;
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(2, length - 2).ToArray(); // trim prefix
        var hex = BitConverter.ToString(result);
        Assert.Equal("37-02-74-72-61-6E-73-6C-61-74-65-07-70-72-69-76-61-74-65", hex);
    }

    [Fact]
    public void BeginEncodeHeaders_NonStaticKey_WriteFullNameAndFullValue_CustomHeader()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers["new-header"] = "value";

        var totalHeaderSize = 0;
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(2, length - 2).ToArray(); // trim prefix
        var hex = BitConverter.ToString(result);
        Assert.Equal("37-03-6E-65-77-2D-68-65-61-64-65-72-05-76-61-6C-75-65", hex);
    }

    [Fact]
    public void BeginEncodeHeaders_StaticKey_WriteStaticNameAndFullValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.ContentType = "application/custom";

        var totalHeaderSize = 0;
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(2, length - 2).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("5F-1D-12-61-70-70-6C-69-63-61-74-69-6F-6E-2F-63-75-73-74-6F-6D", hex);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(9L)]
    [InlineData(299L)]
    [InlineData(300L)]
    [InlineData(1024L)]
    [InlineData(long.MaxValue)]
    public void BeginEncodeHeaders_ContentLength_MatchesStringEncoding(long contentLength)
    {
        var value = contentLength.ToString(CultureInfo.InvariantCulture);

        Span<byte> expected = new byte[64];
        int expectedLength;
        if (contentLength == 0)
        {
            Assert.True(QPackEncoder.EncodeStaticIndexedHeaderField(H3StaticTable.ContentLength0, expected, out expectedLength));
        }
        else
        {
            Assert.True(QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReference(H3StaticTable.ContentLength0, value, expected, out expectedLength));
        }

        foreach (var encodingSelector in new Func<string, Encoding>[] { KestrelServerOptions.DefaultHeaderEncodingSelector, _ => null })
        {
            var headers = new HttpResponseHeaders(encodingSelector);
            headers.ContentLength = contentLength;
            var enumerator = new Http3HeadersEnumerator();
            enumerator.Initialize(headers);

            Span<byte> buffer = new byte[64];
            var totalHeaderSize = 0;
            Assert.True(QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length));
            Assert.Equal(BitConverter.ToString(expected.Slice(0, expectedLength).ToArray()), BitConverter.ToString(buffer.Slice(2, length - 2).ToArray()));
            Assert.Equal(HeaderField.GetLength("Content-Length".Length, value.Length), totalHeaderSize);
        }
    }

    [Theory]
    [InlineData(9L)]
    [InlineData(1024L)]
    [InlineData(long.MaxValue)]
    public void BeginEncodeHeaders_ContentLength_BufferTooSmall_MatchesStringEncoding(long contentLength)
    {
        for (var bufferSize = 2; bufferSize < 32; bufferSize++)
        {
            var fast = Encode(KestrelServerOptions.DefaultHeaderEncodingSelector, bufferSize);
            var slow = Encode(_ => null, bufferSize);
            Assert.Equal(slow, fast);
        }

        (bool, string, int, string) Encode(Func<string, Encoding> encodingSelector, int bufferSize)
        {
            var headers = new HttpResponseHeaders(encodingSelector);
            headers.ContentLength = contentLength;
            ((IHeaderDictionary)headers).ContentType = "text/plain";
            var enumerator = new Http3HeadersEnumerator();
            enumerator.Initialize(headers);

            var buffer = new byte[bufferSize];
            var totalHeaderSize = 0;
            var done = QPackHeaderWriter.BeginEncodeHeaders(200, enumerator, buffer, ref totalHeaderSize, out var length);
            var written = BitConverter.ToString(buffer, 0, length);

            var remaining = new List<string>();
            if (!done)
            {
                do
                {
                    remaining.Add(enumerator.Current.Key + ": " + enumerator.Current.Value);
                } while (enumerator.MoveNext());
            }

            return (done, written, totalHeaderSize, string.Join(";", remaining));
        }
    }
}
