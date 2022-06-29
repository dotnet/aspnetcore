// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportStreamTests : Http3TestBase
{
    public WebTransportStreamTests() : base()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;
    }

    [Theory]
    [InlineData(WebTransportStreamType.Bidirectional, true, true)]
    [InlineData(WebTransportStreamType.Input, true, false)]
    [InlineData(WebTransportStreamType.Output, false, true)]
    internal void WebTransportStream_StreamTypesAreDefinedCorrectly(WebTransportStreamType type, bool canRead, bool canWrite)
    {
        var stream = WebTransportTestUtilities.CreateStream(type);

        Assert.Equal(canRead, stream.CanRead);
        Assert.Equal(canWrite, stream.CanWrite);

        stream.Close();

        // after closing the stream should not be able to read or write, regardless of type
        Assert.False(stream.CanRead);
        Assert.False(stream.CanWrite);
    }

    [Fact]
    internal async Task WebTransportStream_WritingFlushingReadingWorks()
    {
        var memory = new Memory<byte>(new byte[5]);

        var stream = WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional, memory);

        var input = new byte[5] { 0x61, 0x62, 0x63, 0x64, 0x65 };
        await stream.WriteAsync(input, CancellationToken.None);

        await stream.FlushAsync();

        var memoryOut = new Memory<byte>(new byte[5]);
        var length = await stream.ReadAsync(memoryOut, CancellationToken.None);

        Assert.Equal(5, length);
        Assert.Equal(input, memoryOut.ToArray());
    }
}
