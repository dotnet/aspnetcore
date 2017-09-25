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
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class MapEndPointTests
    {
        [Fact]
        public void MapEndPointFindsAuthAttributeOnEndPoint()
        {
            var authCount = 0;
            var builder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSockets();
                    services.AddEndPoint<AuthEndPoint>();
                })
                .Configure(app =>
                {
                    app.UseSockets(routes =>
                    {
                        routes.MapEndPoint<AuthEndPoint>("auth", httpSocketOptions =>
                        {
                            authCount += httpSocketOptions.AuthorizationData.Count;
                        });
                    });
                })
                .Build();

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapEndPointFindsAuthAttributeOnInheritedEndPoint()
        {
            var authCount = 0;
            var builder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSockets();
                    services.AddEndPoint<InheritedAuthEndPoint>();
                })
                .Configure(app =>
                {
                    app.UseSockets(routes =>
                    {
                        routes.MapEndPoint<InheritedAuthEndPoint>("auth", httpSocketOptions =>
                        {
                            authCount += httpSocketOptions.AuthorizationData.Count;
                        });
                    });
                })
                .Build();

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapEndPointFindsAuthAttributesOnDoubleAuthEndPoint()
        {
            var authCount = 0;
            var builder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSockets();
                    services.AddEndPoint<DoubleAuthEndPoint>();
                })
                .Configure(app =>
                {
                    app.UseSockets(routes =>
                    {
                        routes.MapEndPoint<DoubleAuthEndPoint>("auth", httpSocketOptions =>
                        {
                            authCount += httpSocketOptions.AuthorizationData.Count;
                        });
                    });
                })
                .Build();

            Assert.Equal(2, authCount);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task MapEndPointWithWebSocketSubProtocolSetsProtocol()
        {
            var host = new WebHostBuilder()
                .UseUrls("http://127.0.0.1:0")
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSockets();
                    services.AddEndPoint<MyEndPoint>();
                })
                .Configure(app =>
                {
                    app.UseSockets(routes =>
                    {
                        routes.MapEndPoint<MyEndPoint>("socket", httpSocketOptions =>
                        {
                            httpSocketOptions.WebSockets.SubProtocol = "protocol1";
                        });
                    });
                })
                .Build();

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
                while (!await connection.Transport.In.WaitToReadAsync())
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
    }
}
