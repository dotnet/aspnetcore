// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportSessionTests : Http3TestBase
{
    public override void Dispose()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = false;
    }

    [Fact]
    public async Task WebTransportSession_CanOpenNewStream()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

        //verify that we opened an output stream
        Assert.True(stream.CanWrite);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_AcceptNewStreamsInOrderOfArrival()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        // pretend that we received 2 new stream requests from a client
        session.AddStream(WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional));
        session.AddStream(WebTransportTestUtilities.CreateStream(WebTransportStreamType.Input));

        // verify that we accepted a bidirectional stream
        var stream = await session.AcceptStreamAsync(CancellationToken.None);
        Assert.True(stream.CanWrite);
        Assert.True(stream.CanRead);

        // verify that we accepted a unidirectional stream
        var stream2 = await session.AcceptStreamAsync(CancellationToken.None);
        Assert.False(stream2.CanWrite);
        Assert.True(stream2.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_ClosesProperlyOnAbort()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        session.OnClientConnectionClosed();

        // check that all future method calls which are not related to closing throw
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await session.AcceptStreamAsync(CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await session.OpenUnidirectionalStreamAsync(CancellationToken.None));

        var stream = WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional);
        Assert.Throws<ObjectDisposedException>(() => session.AddStream(stream));
    }
}
