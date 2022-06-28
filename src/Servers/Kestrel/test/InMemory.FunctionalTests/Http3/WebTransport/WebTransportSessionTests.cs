// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportSessionTests : Http3TestBase
{
    public WebTransportSessionTests() : base()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", true);
    }

    [Fact]
    public async Task WebTransportSession_CanOpenNewStream()
    {
        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

        //verify that we opened an output stream
        Assert.True(stream.CanWrite);
        Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_AcceptNewStreamsInOrderOfArrival()
    {
        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        // pretend that we received 2 new stream requests from a client
        session.AddStream(WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional));
        session.AddStream(WebTransportTestUtilities.CreateStream(WebTransportStreamType.Input));

        //// verify that we accepted a bidirectional stream
        var stream = await session.AcceptStreamAsync(CancellationToken.None);
        Assert.True(stream.CanWrite);
        Assert.True(stream.CanRead);

        //// verify that we accepted a unidirectional stream
        var stream2 = await session.AcceptStreamAsync(CancellationToken.None);
        Assert.False(stream2.CanWrite);
        Assert.True(stream2.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_ClosesProperlyOnAbort()
    {
        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        session.OnClientConnectionClosed();

        // check that all future method calls which are not related to closing throw
        await Assert.ThrowsAsync<Exception>(async () => await session.AcceptStreamAsync(CancellationToken.None));
        await Assert.ThrowsAsync<Exception>(async () => await session.OpenUnidirectionalStreamAsync(CancellationToken.None));

        var stream = WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional);
        Assert.Throws<Exception>(() => session.AddStream(stream));
    }
}
