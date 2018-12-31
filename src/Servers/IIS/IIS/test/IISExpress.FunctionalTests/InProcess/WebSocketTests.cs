// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(IISTestSiteCollection.Name)]
    [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "No supported on this platform")]
    public class WebSocketsTests
    {
        private readonly string _webSocketUri;

        public WebSocketsTests(IISTestSiteFixture fixture)
        {
            _webSocketUri = fixture.DeploymentResult.ApplicationBaseUri.Replace("http:", "ws:");
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
            await cws.ConnectAsync(new Uri(_webSocketUri + "WebReadBeforeUpgrade"), default);

            await ReceiveMessage(cws, "Yay");
        }

        [ConditionalFact]
        public async Task CanSendAndReceieveData()
        {
            var cws = new ClientWebSocket();
            await cws.ConnectAsync(new Uri(_webSocketUri + "WebSocketEcho"), default);

            for (int i = 0; i < 1000; i++)
            {
                var mesage = i.ToString();
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
