// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "No WebSocket supported on Win7")]
    public class WebSocketsTests
    {
        private readonly string _requestUri;
        private readonly string _webSocketUri;

        public WebSocketsTests(IISTestSiteFixture fixture)
        {
            _requestUri = fixture.DeploymentResult.ApplicationBaseUri;
            _webSocketUri = _requestUri.Replace("http:", "ws:");
        }

        [ConditionalFact]
        public async Task RequestWithBody_NotUpgradable()
        {
            using var client = new HttpClient();
            using var response = await client.PostAsync(_requestUri + "WebSocketNotUpgradable", new StringContent("Hello World"));
            response.EnsureSuccessStatusCode();
        }

        [ConditionalFact]
        public async Task RequestWithoutBody_Upgradable()
        {
            using var client = new HttpClient();
            // POST with Content-Length: 0 counts as not having a body.
            using var response = await client.PostAsync(_requestUri + "WebSocketUpgradable", new StringContent(""));
            response.EnsureSuccessStatusCode();
        }

        [ConditionalFact]
        public async Task OnStartedCalledForWebSocket()
        {
            var cws = new ClientWebSocket();
            await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketLifetimeEvents"), default);

            await ReceiveMessage(cws, "OnStarting");
            await ReceiveMessage(cws, "Upgraded");
        }

        [ConditionalFact]
        public async Task WebReadBeforeUpgrade()
        {
            var cws = new ClientWebSocket();
            await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketReadBeforeUpgrade"), default);

            await ReceiveMessage(cws, "Yay");
        }

        [ConditionalFact]
        public async Task CanSendAndReceieveData()
        {
            var cws = new ClientWebSocket();
            await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketEcho"), default);

            for (int i = 0; i < 1000; i++)
            {
                var mesage = i.ToString(CultureInfo.InvariantCulture);
                await SendMessage(cws, mesage);
                await ReceiveMessage(cws, mesage);
            }
        }

        private async Task SendMessage(ClientWebSocket webSocket, string message)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(message)), WebSocketMessageType.Text, true, default);
        }

        private async Task ReceiveMessage(ClientWebSocket webSocket,  string expectedMessage)
        {
            var received = new byte[expectedMessage.Length];

            var offset = 0;
            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(received, offset, received.Length - offset), default);
                offset += result.Count;
            } while (!result.EndOfMessage);

            Assert.Equal(expectedMessage, Encoding.ASCII.GetString(received));
        }
    }
}
