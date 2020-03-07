// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [Collection(EndToEndTestsCollection.Name)]
    public class WebSocketsTransportTests : FunctionalTestBase
    {
        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public void HttpOptionsSetOntoWebSocketOptions()
        {
            ClientWebSocketOptions webSocketsOptions = null;

            var httpOptions = new HttpConnectionOptions();
            httpOptions.Cookies.Add(new Cookie("Name", "Value", string.Empty, "fakeuri.org"));
            var clientCertificate = new X509Certificate();
            httpOptions.ClientCertificates.Add(clientCertificate);
            httpOptions.UseDefaultCredentials = false;
            httpOptions.Credentials = Mock.Of<ICredentials>();
            httpOptions.Proxy = Mock.Of<IWebProxy>();
            httpOptions.WebSocketConfiguration = options => webSocketsOptions = options;

            var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: httpOptions, loggerFactory: null, accessTokenProvider: null);
            Assert.NotNull(webSocketsTransport);

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
        public async Task WebSocketsTransportStopsSendAndReceiveLoopsWhenTransportIsStopped()
        {
            using (var server = await StartServer<Startup>())
            {
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null);
                await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                    TransferFormat.Binary).OrTimeout();
                await webSocketsTransport.StopAsync().OrTimeout();
                await webSocketsTransport.Running.OrTimeout();
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task WebSocketsTransportSendsUserAgent()
        {
            using (var server = await StartServer<Startup>())
            {
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null);
                await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/httpheader"),
                    TransferFormat.Binary).OrTimeout();

                await webSocketsTransport.Output.WriteAsync(Encoding.UTF8.GetBytes("User-Agent"));

                // The HTTP header endpoint closes the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

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
            using (var server = await StartServer<Startup>())
            {
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null);
                await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/httpheader"),
                    TransferFormat.Binary).OrTimeout();

                await webSocketsTransport.Output.WriteAsync(Encoding.UTF8.GetBytes(HeaderNames.XRequestedWith));

                // The HTTP header endpoint closes the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

                Assert.True(webSocketsTransport.Input.TryRead(out var result));

                var headerValue = Encoding.UTF8.GetString(result.Buffer.ToArray());

                Assert.Equal("XMLHttpRequest", headerValue);
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task WebSocketsTransportStopsWhenConnectionChannelClosed()
        {
            using (var server = await StartServer<Startup>())
            {
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null);
                await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                    TransferFormat.Binary);
                webSocketsTransport.Output.Complete();
                await webSocketsTransport.Running.OrTimeout(TimeSpan.FromSeconds(10));
            }
        }

        [ConditionalTheory]
        [WebSocketsSupportedCondition]
        [InlineData(TransferFormat.Text)]
        [InlineData(TransferFormat.Binary)]
        public async Task WebSocketsTransportStopsWhenConnectionClosedByTheServer(TransferFormat transferFormat)
        {
            using (var server = await StartServer<Startup>())
            {
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null);
                await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echoAndClose"), transferFormat);

                await webSocketsTransport.Output.WriteAsync(new byte[] { 0x42 });

                // The echoAndClose endpoint closes the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

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
            using (var server = await StartServer<Startup>())
            {
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, loggerFactory: LoggerFactory, accessTokenProvider: null);

                await webSocketsTransport.StartAsync(new Uri(server.WebSocketsUrl + "/echo"),
                    transferFormat).OrTimeout();

                await webSocketsTransport.StopAsync().OrTimeout();
                await webSocketsTransport.Running.OrTimeout();
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
                var webSocketsTransport = new WebSocketsTransport(httpConnectionOptions: null, LoggerFactory, accessTokenProvider: null);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    webSocketsTransport.StartAsync(new Uri("http://fakeuri.org"), transferFormat));

                Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
                Assert.Equal("transferFormat", exception.ParamName);
            }
        }
    }
}
