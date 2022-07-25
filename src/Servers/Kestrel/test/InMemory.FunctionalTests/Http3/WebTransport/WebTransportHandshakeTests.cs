// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.AspNetCore.Server.Kestrel.Core.Tests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Http3SettingType = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.Http3SettingType;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportHandshakeTests : Http3TestBase
{
    [Fact]
    public async Task WebTransportHandshake_ClientToServerPasses()
    {
        _serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        var appCompletedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await Http3Api.InitializeConnectionAsync(async context =>
        {
            var success = true;

            var webTransportFeature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();

            success &= webTransportFeature.IsWebTransportRequest;

#pragma warning disable CA2252 // This API requires opting into preview features
            try
            {
                var session = await webTransportFeature.AcceptAsync(CancellationToken.None).DefaultTimeout(); // todo session is null here

                success &= session is not null;

                appCompletedTcs.SetResult(success);
            }
            catch (TimeoutException)
            {
                appCompletedTcs.SetResult(false);
            }
#pragma warning restore CA2252

        });
        var controlStream = await Http3Api.CreateControlStream();
        var controlStream2 = await Http3Api.GetInboundControlStream();

        var settings = new Http3PeerSettings()
        {
            EnableWebTransport = 1,
            H3Datagram = 1,
        };

        await controlStream.SendSettingsAsync(settings.GetNonProtocolDefaults());
        var response1 = await controlStream2.ExpectSettingsAsync();

        await Http3Api.ServerReceivedSettingsReader.ReadAsync().DefaultTimeout();

        Assert.Equal(1, response1[(long)Http3SettingType.EnableWebTransport]);

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(PseudoHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(PseudoHeaderNames.Protocol, "webtransport"),
            new KeyValuePair<string, string>(PseudoHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(PseudoHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(PseudoHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "server.example.com"),
            new KeyValuePair<string, string>(WebTransportSession.CurrentSuppportedVersion, "1")
        });

        var response2 = await requestStream.ExpectHeadersAsync();

        Assert.Equal((int)HttpStatusCode.OK, Convert.ToInt32(response2[PseudoHeaderNames.Status], null));

        await requestStream.OnDisposedTask.DefaultTimeout();
        Assert.True(await appCompletedTcs.Task);
    }

    [Theory]
    [InlineData(
        ((long)Http3ErrorCode.ProtocolError),
        nameof(CoreStrings.Http3MethodMustBeConnectWhenUsingProtocolPseudoHeader),
        nameof(PseudoHeaderNames.Method), "GET", // incorrect method (verifies that webtransport doesn't break regular Http/3 get)
        nameof(PseudoHeaderNames.Protocol), "webtransport",
        nameof(PseudoHeaderNames.Scheme), "http",
        nameof(PseudoHeaderNames.Path), "/",
        nameof(PseudoHeaderNames.Authority), "server.example.com",
        nameof(HeaderNames.Origin), "server.example.com")]
    [InlineData(
        ((long)Http3ErrorCode.ProtocolError),
        nameof(CoreStrings.Http3MissingAuthorityOrPathPseudoHeaders),
        nameof(PseudoHeaderNames.Method), "CONNECT",
        nameof(PseudoHeaderNames.Protocol), "webtransport",
        nameof(PseudoHeaderNames.Scheme), "http",
        nameof(PseudoHeaderNames.Authority), "server.example.com",
        nameof(HeaderNames.Origin), "server.example.com")]  // no path
    [InlineData(
        ((long)Http3ErrorCode.ProtocolError),
        nameof(CoreStrings.Http3MissingAuthorityOrPathPseudoHeaders),
        nameof(PseudoHeaderNames.Method), "CONNECT",
        nameof(PseudoHeaderNames.Protocol), "webtransport",
        nameof(PseudoHeaderNames.Scheme), "http",
        nameof(PseudoHeaderNames.Path), "/",
        nameof(HeaderNames.Origin), "server.example.com")]  // no authority
    public async Task WebTransportHandshake_IncorrectHeadersRejects(long error, string targetErrorMessage, params string[] headers)
    {
        _serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = true;

        await Http3Api.InitializeConnectionAsync(_noopApplication);
        var controlStream = await Http3Api.CreateControlStream();
        var controlStream2 = await Http3Api.GetInboundControlStream();

        var settings = new Http3PeerSettings()
        {
            EnableWebTransport = 1,
            H3Datagram = 1,
        };

        await controlStream.SendSettingsAsync(settings.GetNonProtocolDefaults());
        var response1 = await controlStream2.ExpectSettingsAsync();

        await Http3Api.ServerReceivedSettingsReader.ReadAsync().DefaultTimeout();

        Assert.Equal(1, response1[(long)Http3SettingType.EnableWebTransport]);

        var headersConnectFrame = new List<KeyValuePair<string, string>>();
        for (var i = 0; i < headers.Length; i += 2)
        {
            headersConnectFrame.Add(new KeyValuePair<string, string>(GetHeaderFromName(headers[i]), headers[i + 1]));
        }

        var requestStream = await Http3Api.CreateRequestStream(headersConnectFrame);

        await requestStream.WaitForStreamErrorAsync((Http3ErrorCode)error, AssertExpectedErrorMessages, GetCoreStringFromName(targetErrorMessage));
    }

    private static string GetCoreStringFromName(string headerName)
    {
        return headerName switch
        {
            nameof(CoreStrings.Http3MissingAuthorityOrPathPseudoHeaders) => CoreStrings.Http3MissingAuthorityOrPathPseudoHeaders,
            nameof(CoreStrings.Http3MethodMustBeConnectWhenUsingProtocolPseudoHeader) => CoreStrings.Http3MethodMustBeConnectWhenUsingProtocolPseudoHeader,
            _ => throw new Exception("Core string not mapped yet")
        };
    }

    private static string GetHeaderFromName(string coreStringName)
    {
        return coreStringName switch
        {
            nameof(PseudoHeaderNames.Method) => PseudoHeaderNames.Method,
            nameof(PseudoHeaderNames.Protocol) => PseudoHeaderNames.Protocol,
            nameof(PseudoHeaderNames.Scheme) => PseudoHeaderNames.Scheme,
            nameof(PseudoHeaderNames.Path) => PseudoHeaderNames.Path,
            nameof(PseudoHeaderNames.Authority) => PseudoHeaderNames.Authority,
            nameof(HeaderNames.Origin) => HeaderNames.Origin,
            _ => throw new Exception("Header name not mapped yet")
        };
    }
}
