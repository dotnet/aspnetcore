// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebSockets.Tests;

public class HandshakeTests
{
    [Fact]
    public void CreatesCorrectResponseKey()
    {
        // Example taken from https://tools.ietf.org/html/rfc6455#section-1.3
        var key = "dGhlIHNhbXBsZSBub25jZQ==";
        var expectedResponse = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

        var response = HandshakeHelpers.CreateResponseKey(key);

        Assert.Equal(expectedResponse, response);
    }

    [Theory]
    [InlineData("VUfWn1u2Ot0AICM6f+/8Zg==")]
    public void AcceptsValidRequestKeys(string key)
    {
        Assert.True(HandshakeHelpers.IsRequestKeyValid(key));
    }

    [Theory]
    // 17 bytes when decoded
    [InlineData("dGhpcyBpcyAxNyBieXRlcy4=")]
    // 15 bytes when decoded
    [InlineData("dGhpcyBpcyAxNWJ5dGVz")]
    [InlineData("")]
    [InlineData("24 length not base64 str")]
    public void RejectsInvalidRequestKeys(string key)
    {
        Assert.False(HandshakeHelpers.IsRequestKeyValid(key));
    }

    [Theory]
    [InlineData("permessage-deflate", "permessage-deflate")]
    [InlineData("permessage-deflate; server_no_context_takeover", "permessage-deflate; server_no_context_takeover")]
    [InlineData("permessage-deflate; client_no_context_takeover", "permessage-deflate; client_no_context_takeover")]
    [InlineData("permessage-deflate; client_max_window_bits=9", "permessage-deflate; client_max_window_bits=9")]
    [InlineData("permessage-deflate; client_max_window_bits=\"9\"", "permessage-deflate; client_max_window_bits=9")]
    [InlineData("permessage-deflate; client_max_window_bits", "permessage-deflate; client_max_window_bits=15")]
    [InlineData("permessage-deflate; server_max_window_bits", "permessage-deflate; server_max_window_bits=15")]
    [InlineData("permessage-deflate; server_max_window_bits=10", "permessage-deflate; server_max_window_bits=10")]
    [InlineData("permessage-deflate; server_max_window_bits=10; server_no_context_takeover", "permessage-deflate; server_no_context_takeover; server_max_window_bits=10")]
    [InlineData("permessage-deflate; server_max_window_bits=10; server_no_context_takeover; client_no_context_takeover; client_max_window_bits=12", "permessage-deflate; client_no_context_takeover; client_max_window_bits=12; server_no_context_takeover; server_max_window_bits=10")]
    public void CompressionNegotiationProducesCorrectHeaderWithDefaultOptions(string clientHeader, string expectedResponse)
    {
        Assert.True(HandshakeHelpers.ParseDeflateOptions(clientHeader.AsSpan(), serverContextTakeover: true, serverMaxWindowBits: 15,
            out var _, out var response));
        Assert.Equal(expectedResponse, response);
    }

    [Theory]
    [InlineData("permessage-deflate", "permessage-deflate; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; server_no_context_takeover", "permessage-deflate; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; client_no_context_takeover", "permessage-deflate; client_no_context_takeover; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; client_max_window_bits=9", "permessage-deflate; client_max_window_bits=9; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; client_max_window_bits", "permessage-deflate; client_max_window_bits=15; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; server_max_window_bits", "permessage-deflate; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; server_max_window_bits=14", "permessage-deflate; server_no_context_takeover; server_max_window_bits=14")]
    [InlineData("permessage-deflate; server_max_window_bits=10", "permessage-deflate; server_no_context_takeover; server_max_window_bits=10")]
    [InlineData("permessage-deflate; server_max_window_bits=10; server_no_context_takeover", "permessage-deflate; server_no_context_takeover; server_max_window_bits=10")]
    [InlineData("permessage-deflate; server_max_window_bits=10; client_no_context_takeover; client_max_window_bits=12", "permessage-deflate; client_no_context_takeover; client_max_window_bits=12; server_no_context_takeover; server_max_window_bits=10")]
    public void CompressionNegotiationProducesCorrectHeaderWithCustomOptions(string clientHeader, string expectedResponse)
    {
        Assert.True(HandshakeHelpers.ParseDeflateOptions(clientHeader.AsSpan(), serverContextTakeover: false, serverMaxWindowBits: 14,
            out var _, out var response));
        Assert.Equal(expectedResponse, response);
    }

    [Theory]
    [InlineData("permessage-deflate; server_max_window_bits=8")]
    [InlineData("permessage-deflate; client_max_window_bits=8")]
    [InlineData("permessage-deflate; server_max_window_bits=16")]
    [InlineData("permessage-deflate; client_max_window_bits=16")]
    [InlineData("permessage-deflate; client_max_window_bits=\"15")]
    [InlineData("permessage-deflate; client_max_window_bits=14\"")]
    [InlineData("permessage-deflate; client_max_window_bits=\"")]
    [InlineData("permessage-deflate; client_max_window_bits=\"13")]
    [InlineData("permessage-deflate; client_max_window_bits=")]
    [InlineData("permessage-deflate; client_max_window_bits=\"\"")]
    [InlineData("permessage-deflate; client_max_window_bits=14; client_max_window_bits=14")]
    [InlineData("permessage-deflate; server_max_window_bits=14; server_max_window_bits=14")]
    [InlineData("permessage-deflate; server_no_context_takeover; server_no_context_takeover")]
    [InlineData("permessage-deflate; client_no_context_takeover; client_no_context_takeover")]
    public void CompressionNegotiateNotAccepted(string clientHeader)
    {
        Assert.False(HandshakeHelpers.ParseDeflateOptions(clientHeader.AsSpan(), serverContextTakeover: true, serverMaxWindowBits: 15,
            out var _, out var response));
    }
}
