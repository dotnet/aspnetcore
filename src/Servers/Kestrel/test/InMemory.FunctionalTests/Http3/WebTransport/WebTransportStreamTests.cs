// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportStreamTests : Http3TestBase
{
    private static readonly byte[] RandomBytes = new byte[5] { 0x61, 0x62, 0x63, 0x64, 0x65 };

    [Theory]
    [InlineData(WebTransportStreamType.Bidirectional, true, true)]
    [InlineData(WebTransportStreamType.Input, true, false)]
    [InlineData(WebTransportStreamType.Output, false, true)]
    internal async Task WebTransportStream_StreamTypesAreDefinedCorrectly(WebTransportStreamType type, bool canRead, bool canWrite)
    {
        var memory = new Memory<byte>(new byte[5]);
        var stream = WebTransportTestUtilities.CreateStream(type, memory);

        var streamDirectionFeature = stream.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.Equal(canRead, streamDirectionFeature.CanRead);
        Assert.Equal(canWrite, streamDirectionFeature.CanWrite);

        await stream.DisposeAsync();

        // test that you can't write or read from a stream after disposing
        Assert.False(streamDirectionFeature.CanRead);
        Assert.False(streamDirectionFeature.CanWrite);
    }

    [Fact]
    internal async Task WebTransportStream_WritingFlushingReadingWorks()
    {
        var memory = new Memory<byte>(new byte[5]);

        var stream = WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional, memory);

        var input = new ReadOnlyMemory<byte>(RandomBytes);
        await stream.Transport.Output.WriteAsync(input, CancellationToken.None);

        await stream.Transport.Output.FlushAsync();

        var memoryOut = new Memory<byte>(new byte[5]);
        var length = await stream.Transport.Input.AsStream().ReadAsync(memoryOut, CancellationToken.None);

        Assert.Equal(5, length);
        Assert.Equal(input.ToArray(), memoryOut.ToArray());
    }
}
