// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportSessionTests : Http3TestBase
{
    [Fact]
    public async Task WebTransportSession_CanOpenNewStream()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

        //verify that we opened an output stream
        var streamDirectionFeature = stream.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.True(streamDirectionFeature.CanWrite);
        Assert.False(streamDirectionFeature.CanRead);
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
        var streamDirectionFeature = stream.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.True(streamDirectionFeature.CanWrite);
        Assert.True(streamDirectionFeature.CanRead);

        // verify that we accepted a unidirectional stream
        var stream2 = await session.AcceptStreamAsync(CancellationToken.None);
        var streamDirectionFeature2 = stream2.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.False(streamDirectionFeature2.CanWrite);
        Assert.True(streamDirectionFeature2.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_ClosesProperlyOnAbort()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        session.OnClientConnectionClosed();

        // check that all future method calls which are not related to closing throw
        Assert.Null(await session.AcceptStreamAsync(CancellationToken.None));
        Assert.Null(await session.OpenUnidirectionalStreamAsync(CancellationToken.None));

        // doublec check that no exceptions are thrown
        var _ = WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional);
    }
}
