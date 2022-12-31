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

        var exitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var session = await WebTransportTestUtilities.GenerateSession(Http3Api, exitTcs);

        var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

        //verify that we opened an output stream
        Assert.NotNull(stream);
        var streamDirectionFeature = stream.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.True(streamDirectionFeature.CanWrite);
        Assert.False(streamDirectionFeature.CanRead);

        // end the application
        exitTcs.SetResult();
    }

    [Fact]
    public async Task WebTransportSession_AcceptNewStreamsInOrderOfArrival()
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true; // TODO add more sync code as now it is flaky

        var exitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = await WebTransportTestUtilities.GenerateSession(Http3Api, exitTcs);

        // pretend that we received 2 new stream requests from a client
        session.AddStream(WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional));
        session.AddStream(WebTransportTestUtilities.CreateStream(WebTransportStreamType.Input));

        var stream = await session.AcceptStreamAsync(CancellationToken.None);

        // verify that we accepted a bidirectional stream
        Assert.NotNull(stream);
        var streamDirectionFeature = stream.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.True(streamDirectionFeature.CanWrite);
        Assert.True(streamDirectionFeature.CanRead);

        var stream2 = await session.AcceptStreamAsync(CancellationToken.None);

        // verify that we accepted a unidirectional stream
        Assert.NotNull(stream2);
        var streamDirectionFeature2 = stream2.Features.GetRequiredFeature<IStreamDirectionFeature>();
        Assert.False(streamDirectionFeature2.CanWrite);
        Assert.True(streamDirectionFeature2.CanRead);

        exitTcs.SetResult();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task WebTransportSession_ClosesProperly(int method)
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var exitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = await WebTransportTestUtilities.GenerateSession(Http3Api, exitTcs);

        switch (method)
        {
            case 0: // manual abort
                session.Abort(new(), System.Net.Http.Http3ErrorCode.InternalError);
                break;
            case 1: // manual graceful close
                session.OnClientConnectionClosed();
                break;
            case 2: // automatic graceful close due to application and connection ending
                exitTcs.SetResult();
                break;
            case 3: // automatic abort due to host stream aborting
                Http3Api.Connection._streams[session.SessionId].Abort(new(), System.Net.Http.Http3ErrorCode.InternalError);
                break;
        }

        // check that all future method calls which are not related to closing throw
        Assert.Null(await session.AcceptStreamAsync(CancellationToken.None));
        Assert.Null(await session.OpenUnidirectionalStreamAsync(CancellationToken.None));

        // doublec check that no exceptions are thrown
        var _ = WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional);

        exitTcs.TrySetResult();
    }
}
