// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Http3SettingType = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.Http3SettingType;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportTests : Http3TestBase
{
    [Fact]
    public async Task WebTransportHandshake_ClientToServerPasses()
    {
        _serviceContext.ServerOptions.EnableWebTransport = true;
        _serviceContext.ServerOptions.EnableHttp3Datagrams = true;

        await Http3Api.InitializeConnectionAsync(_noopApplication);
        var controlStream = await Http3Api.CreateControlStream();
        var controlStream2 = await Http3Api.GetInboundControlStream();

        var settings = new Http3PeerSettings()
        {
            EnableWebTransport = 1,
            H3Datagram = 1
        };

        await controlStream.SendSettingsAsync(settings.GetNonProtocolDefaults());
        var response1 = await controlStream2.ExpectSettingsAsync();

        await Http3Api.ServerReceivedSettingsReader.ReadAsync().DefaultTimeout();

        Assert.Equal(settings.EnableWebTransport, response1[(long)Http3SettingType.EnableWebTransport]);
        Assert.Equal(settings.H3Datagram, response1[(long)Http3SettingType.H3Datagram]);


        var requestStream = await Http3Api.CreateRequestStream();
        var headersConnectFrame = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "webtransport"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "server.example.com")
        };

        await requestStream.SendHeadersAsync(headersConnectFrame);
        var response2 = await requestStream.ExpectHeadersAsync();

        Assert.Equal((int)HttpStatusCode.OK, Convert.ToInt32(response2[HeaderNames.Status]));

        await requestStream.OnDisposedTask.DefaultTimeout();
    }


    [Fact]
    public void WebTransport_WhenOffRequests404()
    {
        // TODO check that if webtransport is off, it does not allow clients to start sessions and connect to it via webtransport
    }

    [Fact]
    public void WebTransportHandshake_IncorrectHeadersRejects()
    {
        // TODO check that if any of the pseudoheaders or the origin header are missing from client's message , server will reject the connection (status 404)

        /* todo validate the following things:
           *  SETTINGS_ENABLE_WEBTRANSPORT = 1 on client as well as on server
           *  SETTINGS_H3_DATAGRAM = 1 on client as well as on server
           *  client's initial_max_bidi_streams = 0
           *  (HTTP/2 only) set the server's SETTINGS_ENABLE_CONNECT_PROTOCOL = 1
           *  :protocol = webtransport (given as this is the conditional here)
           *  :scheme = https (http not allowed)
           *  :authority and :path are set
           *  Origin is set
        */

    }

    [Fact]
    public void WebTransportHandshake_NoDatagramNegotiatedRejects()
    {
        // TODO test where client sends message with enable webtransport set but the h3_datagram not set (undefined), server should throw H3_SETTINGS_ERROR error.
    }

    [Fact]
    public void WebTransportHandshake_ZeroMaxStreams_Rejects()
    {
        // TODO test where client sends message with enable webtransport settings are set but the initial_max_bidi_streams tranport parameter is 0. Server should return a H3_SETTINGS_ERROR error.
    }
}
