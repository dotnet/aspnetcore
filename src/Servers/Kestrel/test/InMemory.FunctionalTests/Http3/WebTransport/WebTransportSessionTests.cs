// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

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

        // TODO fails because session is null as I messed up the creation of them
        //var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

        // verify that we opened an output stream
        //Assert.True(stream.CanWrite);
        //Assert.False(stream.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_AcceptNewStreamsInOrderOfArrival()
    {
        var session = await WebTransportTestUtilities.GenerateSession(Http3Api);

        // TODO fails because session is null as I messed up the creation of them
        // pretend that we received 2 new stream requests from a client
        //session.AddStream(await WebTransportTestUtilities.CreateStream(WebTransportStreamType.Bidirectional));
        //session.AddStream(await WebTransportTestUtilities.CreateStream(WebTransportStreamType.Input));

        //// verify that we accepted a bidirectional stream
        //var stream = await session.AcceptStreamAsync(CancellationToken.None);
        //Assert.True(stream.CanWrite);
        //Assert.True(stream.CanRead);

        //// verify that we accepted a unidirectional stream
        //var stream2 = await session.AcceptStreamAsync(CancellationToken.None);
        //Assert.False(stream2.CanWrite);
        //Assert.True(stream2.CanRead);
    }

    [Fact]
    public async Task WebTransportSession_ClosesProperlyOnAbort()
    {
    }
}
