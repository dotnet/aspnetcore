// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

[Collection(EndToEndTestsCollection.Name)]
public class WebSocketsTransportTests : FunctionalTestBase
{
    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task HttpOptionsSetOntoWebSocketOptions()
    {
        ClientWebSocketOptions webSocketsOptions = null;

        var httpOptions = new HttpConnectionOptions();
        httpOptions.Cookies.Add(new Cookie("Name", "Value", string.Empty, "fakeuri.org"));
        var clientCertificate = new X509Certificate(Array.Empty<byte>());
        httpOptions.ClientCertificates.Add(clientCertificate);
        httpOptions.UseDefaultCredentials = false;
        httpOptions.Credentials = Mock.Of<ICredentials>();
        httpOptions.Proxy = Mock.Of<IWebProxy>();
        httpOptions.WebSocketConfiguration = options => webSocketsOptions = options;

        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: httpOptions, loggerFactory: null, accessTokenProvider: null, httpClient: null);
            Assert.NotNull(webSocketsTransport);

            // we need to open a connection so it would apply httpOptions to webSocketOptions
            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                TransferFormat.Binary).DefaultTimeout();
            await webSocketsTransport.StopAsync().DefaultTimeout();
        }

        Assert.NotNull(webSocketsOptions);
        Assert.Equal(1, webSocketsOptions.Cookies.Count);
        Assert.Single(webSocketsOptions.ClientCertificates);
        Assert.Same(clientCertificate, webSocketsOptions.ClientCertificates[0]);
        Assert.False(webSocketsOptions.UseDefaultCredentials);
        Assert.Same(httpOptions.Proxy, webSocketsOptions.Proxy);
        Assert.Same(httpOptions.Credentials, webSocketsOptions.Credentials);
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task HttpOptionsWebSocketFactoryIsUsed()
    {
        var httpOptions = new HttpConnectionOptions();
        var webSocketMock = new Mock<WebSocket>();
        bool factoryWasUsed = false;

        // we emulate that connection is closed right away after it was established
        webSocketMock.Setup(socket => socket.CloseStatus).Returns(WebSocketCloseStatus.NormalClosure);
        webSocketMock.Setup(socket => socket.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));

        httpOptions.WebSocketFactory = (context, token) =>
        {
            factoryWasUsed = true;
            return ValueTask.FromResult(webSocketMock.Object);
        };

        var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: httpOptions, loggerFactory: null, accessTokenProvider: null, httpClient: null);
        await webSocketsTransport.StartAsync(new Uri("http://FakeEndpot.com/echo"), TransferFormat.Binary).DefaultTimeout();
        await webSocketsTransport.StopAsync().DefaultTimeout();

        webSocketMock.Verify((socket) => socket.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once());
        Assert.True(factoryWasUsed);
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task WebSocketsTransportStopsSendAndReceiveLoopsWhenTransportIsStopped()
    {
        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null, httpClient: null);
            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                TransferFormat.Binary).DefaultTimeout();
            await webSocketsTransport.StopAsync().DefaultTimeout();
            await webSocketsTransport.Running.DefaultTimeout();
        }
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task WebSocketsTransportSendsUserAgent()
    {
        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null, httpClient: null);
            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/httpheader"),
                TransferFormat.Binary).DefaultTimeout();

            await webSocketsTransport.Output.WriteAsync(Encoding.UTF8.GetBytes("User-Agent"));

            // The HTTP header endpoint closes the connection immediately after sending response which should stop the transport
            await webSocketsTransport.Running.DefaultTimeout();

            Assert.True(webSocketsTransport.Input.TryRead(out var result));

            var userAgent = Encoding.UTF8.GetString(result.Buffer.ToArray());

            // user agent version should come from version embedded in assembly metadata
            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            var majorVersion = typeof(HttpConnection).Assembly.GetName().Version.Major;
            var minorVersion = typeof(HttpConnection).Assembly.GetName().Version.Minor;

            Assert.StartsWith($"Microsoft SignalR/{majorVersion}.{minorVersion} ({assemblyVersion.InformationalVersion}; ", userAgent);
        }
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task WebSocketsTransportSendsXRequestedWithHeader()
    {
        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null, httpClient: null);
            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/httpheader"),
                TransferFormat.Binary).DefaultTimeout();

            await webSocketsTransport.Output.WriteAsync(Encoding.UTF8.GetBytes(HeaderNames.XRequestedWith));

            // The HTTP header endpoint closes the connection immediately after sending response which should stop the transport
            await webSocketsTransport.Running.DefaultTimeout();

            Assert.True(webSocketsTransport.Input.TryRead(out var result));

            var headerValue = Encoding.UTF8.GetString(result.Buffer.ToArray());

            Assert.Equal("XMLHttpRequest", headerValue);
        }
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task WebSocketsTransportStopsWhenConnectionChannelClosed()
    {
        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null, httpClient: null);
            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                TransferFormat.Binary);
            webSocketsTransport.Output.Complete();
            await webSocketsTransport.Running.DefaultTimeout(TimeSpan.FromSeconds(10));
        }
    }

    [ConditionalTheory]
    [WebSocketsSupportedCondition]
    [InlineData(TransferFormat.Text)]
    [InlineData(TransferFormat.Binary)]
    public async Task WebSocketsTransportStopsWhenConnectionClosedByTheServer(TransferFormat transferFormat)
    {
        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null, httpClient: null);
            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echoAndClose"), transferFormat);

            await webSocketsTransport.Output.WriteAsync(new byte[] { 0x42 });

            // The echoAndClose endpoint closes the connection immediately after sending response which should stop the transport
            await webSocketsTransport.Running.DefaultTimeout();

            Assert.True(webSocketsTransport.Input.TryRead(out var result));
            Assert.Equal(new byte[] { 0x42 }, result.Buffer.ToArray());
            webSocketsTransport.Input.AdvanceTo(result.Buffer.End);
        }
    }

    [ConditionalTheory]
    [WebSocketsSupportedCondition]
    [InlineData(TransferFormat.Text)]
    [InlineData(TransferFormat.Binary)]
    public async Task WebSocketsTransportSetsTransferFormat(TransferFormat transferFormat)
    {
        await using (var server = await StartServer<Startup>())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null, httpClient: null);

            await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                transferFormat).DefaultTimeout();

            await webSocketsTransport.StopAsync().DefaultTimeout();
            await webSocketsTransport.Running.DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [InlineData(TransferFormat.Text | TransferFormat.Binary)] // Multiple values not allowed
    [InlineData((TransferFormat)42)] // Unexpected value
    [WebSocketsSupportedCondition]
    public async Task WebSocketsTransportThrowsForInvalidTransferFormat(TransferFormat transferFormat)
    {
        using (StartVerifiableLog())
        {
            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, LoggerFactory, accessTokenProvider: null, httpClient: null);
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                webSocketsTransport.StartAsync(new Uri("http://fakeuri.org"), transferFormat));

            Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
            Assert.Equal("transferFormat", exception.ParamName);
        }
    }
}
