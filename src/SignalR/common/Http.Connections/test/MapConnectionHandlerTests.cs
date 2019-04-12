// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class MapConnectionHandlerTests
    {
        private readonly ITestOutputHelper _output;

        public MapConnectionHandlerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MapConnectionHandlerFindsAuthAttributeOnEndPoint()
        {
            var authCount = 0;
            using (var builder = BuildWebHost<AuthConnectionHandler>("/auth",
                options => authCount += options.AuthorizationData.Count))
            {
                builder.Start();
            }

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapConnectionHandlerFindsAuthAttributeOnInheritedEndPoint()
        {
            var authCount = 0;
            using (var builder = BuildWebHost<InheritedAuthConnectionHandler>("/auth",
                options => authCount += options.AuthorizationData.Count))
            {
                builder.Start();
            }

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapConnectionHandlerFindsAuthAttributesOnDoubleAuthEndPoint()
        {
            var authCount = 0;
            using (var builder = BuildWebHost<DoubleAuthConnectionHandler>("/auth",
                options => authCount += options.AuthorizationData.Count))
            {
                builder.Start();
            }

            Assert.Equal(2, authCount);
        }

        [Fact]
        public void MapConnectionHandlerEndPointRoutingFindsAttributesOnHub()
        {
            var authCount = 0;
            using (var host = BuildWebHostWithEndPointRouting(routes => routes.MapConnectionHandler<AuthConnectionHandler>("/path", options =>
            {
                authCount += options.AuthorizationData.Count;
            })))
            {
                host.Start();

                var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
                // We register 2 endpoints (/negotiate and /)
                Assert.Equal(2, dataSource.Endpoints.Count);
                Assert.NotNull(dataSource.Endpoints[0].Metadata.GetMetadata<IAuthorizeData>());
                Assert.NotNull(dataSource.Endpoints[1].Metadata.GetMetadata<IAuthorizeData>());
            }

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapConnectionHandlerEndPointRoutingAppliesAttributesBeforeConventions()
        {
            void ConfigureRoutes(IEndpointRouteBuilder endpoints)
            {
                // This "Foo" policy should override the default auth attribute
                endpoints.MapConnectionHandler<AuthConnectionHandler>("/path")
                      .RequireAuthorization(new AuthorizeAttribute("Foo"));
            }

            using (var host = BuildWebHostWithEndPointRouting(ConfigureRoutes))
            {
                host.Start();

                var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
                // We register 2 endpoints (/negotiate and /)
                Assert.Equal(2, dataSource.Endpoints.Count);
                Assert.Equal("Foo", dataSource.Endpoints[0].Metadata.GetMetadata<IAuthorizeData>()?.Policy);
                Assert.Equal("Foo", dataSource.Endpoints[1].Metadata.GetMetadata<IAuthorizeData>()?.Policy);
            }
        }

        [Fact]
        public void MapConnectionHandlerEndPointRoutingAppliesNegotiateMetadata()
        {
            void ConfigureRoutes(IEndpointRouteBuilder endpoints)
            {
                endpoints.MapConnectionHandler<AuthConnectionHandler>("/path");
            }

            using (var host = BuildWebHostWithEndPointRouting(ConfigureRoutes))
            {
                host.Start();

                var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
                // We register 2 endpoints (/negotiate and /)
                Assert.Equal(2, dataSource.Endpoints.Count);
                Assert.NotNull(dataSource.Endpoints[0].Metadata.GetMetadata<NegotiateMetadata>());
                Assert.Null(dataSource.Endpoints[1].Metadata.GetMetadata<NegotiateMetadata>());
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task MapConnectionHandlerWithWebSocketSubProtocolSetsProtocol()
        {
            var host = BuildWebHost<MyConnectionHandler>("/socket",
                options => options.WebSockets.SubProtocolSelector = subprotocols =>
                {
                    Assert.Equal(new [] { "protocol1", "protocol2" }, subprotocols.ToArray());
                    return "protocol1";
                });

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

        private class MyConnectionHandler : ConnectionHandler
        {
            public override async Task OnConnectedAsync(ConnectionContext connection)
            {
                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync();

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    // Consume nothing
                    connection.Transport.Input.AdvanceTo(result.Buffer.Start);
                }
            }
        }

        private class InheritedAuthConnectionHandler : AuthConnectionHandler
        {
            public override Task OnConnectedAsync(ConnectionContext connection)
            {
                throw new NotImplementedException();
            }
        }

        [Authorize]
        private class DoubleAuthConnectionHandler : AuthConnectionHandler
        {
        }

        [Authorize]
        private class AuthConnectionHandler : ConnectionHandler
        {
            public override Task OnConnectedAsync(ConnectionContext connection)
            {
                throw new NotImplementedException();
            }
        }

        private IWebHost BuildWebHostWithEndPointRouting(Action<IEndpointRouteBuilder> configure)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddConnections();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => configure(endpoints));
                })
                .UseUrls("http://127.0.0.1:0")
                .Build();
        }

        private IWebHost BuildWebHost<TConnectionHandler>(string path, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler
        {
            return new WebHostBuilder()
                .UseUrls("http://127.0.0.1:0")
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddConnections();
                })
                .Configure(app =>
                {
                    app.UseConnections(routes =>
                    {
                        routes.MapConnectionHandler<TConnectionHandler>(path, configureOptions);
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
