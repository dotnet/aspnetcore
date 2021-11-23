// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(302, enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(0, length).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("00-00-5F-30-03-33-30-32", hex);
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

    [Fact]
    public void BeginEncodeHeaders_NonStaticKey_WriteFullNameAndFullValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.Translate = "private";

        var totalHeaderSize = 0;
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(302, enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(8, length - 8).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("37-02-74-72-61-6E-73-6C-61-74-65-07-70-72-69-76-61-74-65", hex);
    }

    [Fact]
    public void BeginEncodeHeaders_NoStatus_NonStaticKey_WriteFullNameAndFullValue()
    {
        Span<byte> buffer = new byte[1024 * 16];

        var headers = (IHeaderDictionary)new HttpResponseHeaders();
        headers.Translate = "private";

        var totalHeaderSize = 0;
        var enumerator = new Http3HeadersEnumerator();
        enumerator.Initialize(headers);

        Assert.True(QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length));

        var result = buffer.Slice(2, length - 2).ToArray();
        var hex = BitConverter.ToString(result);
        Assert.Equal("37-02-74-72-61-6E-73-6C-61-74-65-07-70-72-69-76-61-74-65", hex);
    }
}
