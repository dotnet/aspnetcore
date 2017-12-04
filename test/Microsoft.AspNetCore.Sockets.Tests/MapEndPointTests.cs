// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class MapEndPointTests
    {
        private ITestOutputHelper _output;

        public MapEndPointTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MapEndPointFindsAuthAttributeOnEndPoint()
        {
            var authCount = 0;
            using (var builder = BuildWebHost<AuthEndPoint>("auth",
                options => authCount += options.AuthorizationData.Count))
            {
                builder.Start();
            }

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapEndPointFindsAuthAttributeOnInheritedEndPoint()
        {
            var authCount = 0;
            using (var builder = BuildWebHost<InheritedAuthEndPoint>("auth",
                options => authCount += options.AuthorizationData.Count))
            {
                builder.Start();
            }

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapEndPointFindsAuthAttributesOnDoubleAuthEndPoint()
        {
            var authCount = 0;
            using (var builder = BuildWebHost<DoubleAuthEndPoint>("auth",
                options => authCount += options.AuthorizationData.Count))
            {
                builder.Start();
            }

            Assert.Equal(2, authCount);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task MapEndPointWithWebSocketSubProtocolSetsProtocol()
        {
            var host = BuildWebHost<MyEndPoint>("socket",
                options => options.WebSockets.SubProtocol = "protocol1");

            await host.StartAsync();

            var feature = host.ServerFeatures.Get<IServerAddressesFeature>();
            var address = feature.Addresses.First().Replace("http", "ws") + "/socket";

            var client = new ClientWebSocket();
            client.Options.AddSubProtocol("protocol1");
            client.Options.AddSubProtocol("protocol2");
            await client.ConnectAsync(new Uri(address), CancellationToken.None);
            Assert.Equal("protocol1", client.SubProtocol);
            await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).OrTimeout();
            var result = await client.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None).OrTimeout();
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        }

        private class MyEndPoint : EndPoint
        {
            public override async Task OnConnectedAsync(ConnectionContext connection)
            {
                while (!await connection.Transport.Reader.WaitToReadAsync())
                {

                }
            }
        }

        private class InheritedAuthEndPoint : AuthEndPoint
        {
            public override Task OnConnectedAsync(ConnectionContext connection)
            {
                throw new NotImplementedException();
            }
        }

        [Authorize]
        private class DoubleAuthEndPoint : AuthEndPoint
        {
        }

        [Authorize]
        private class AuthEndPoint : EndPoint
        {
            public override Task OnConnectedAsync(ConnectionContext connection)
            {
                throw new NotImplementedException();
            }
        }

        private IWebHost BuildWebHost<TEndPoint>(string path, Action<HttpSocketOptions> configure) where TEndPoint : EndPoint
        {
            return new WebHostBuilder()
                .UseUrls("http://127.0.0.1:0")
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSockets();
                    services.AddEndPoint<TEndPoint>();
                })
                .Configure(app =>
                {
                    app.UseSockets(routes =>
                    {
                        routes.MapEndPoint<TEndPoint>(path,
                            httpSocketOptions => configure(httpSocketOptions));
                    });
                })
                .ConfigureLogging(factory =>
                {
                    factory.AddXunit(_output, LogLevel.Trace);
                })
                .Build();
        }
    }
}
