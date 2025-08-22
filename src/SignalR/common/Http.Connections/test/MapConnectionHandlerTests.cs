// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class MapConnectionHandlerTests
{
    private readonly ITestOutputHelper _output;

    public MapConnectionHandlerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void MapConnectionHandlerFindsMetadataPolicyOnEndPoint()
    {
        var authCount = 0;
        var policy1 = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        var req = new TestRequirement();
        using (var host = BuildWebHost<AuthConnectionHandler>("/auth",
            options => authCount += options.AuthorizationData.Count,
            endpoints => endpoints.RequireAuthorization(policy1).RequireAuthorization(pb => pb.Requirements.Add(req))))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/auth/negotiate", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                    var policies = endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>();
                    Assert.Equal(2, policies.Count);
                    Assert.Equal(policy1, policies[0]);
                    Assert.Single(policies[1].Requirements);
                    Assert.Equal(req, policies[1].Requirements.First());
                },
                endpoint =>
                {
                    Assert.Equal("/auth", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                    var policies = endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>();
                    Assert.Equal(2, policies.Count);
                    Assert.Equal(policy1, policies[0]);
                    Assert.Single(policies[1].Requirements);
                    Assert.Equal(req, policies[1].Requirements.First());
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapConnectionHandlerFindsAuthAttributeOnEndPoint()
    {
        var authCount = 0;
        using (var host = BuildWebHost<AuthConnectionHandler>("/auth",
            options => authCount += options.AuthorizationData.Count))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/auth/negotiate", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                },
                endpoint =>
                {
                    Assert.Equal("/auth", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapConnectionHandlerFindsAuthAttributeOnInheritedEndPoint()
    {
        var authCount = 0;
        using (var host = BuildWebHost<InheritedAuthConnectionHandler>("/auth",
            options => authCount += options.AuthorizationData.Count))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/auth/negotiate", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                },
                endpoint =>
                {
                    Assert.Equal("/auth", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]

    public void MapConnectionHandlerFindsAuthAttributesOnDoubleAuthEndPoint()
    {
        var authCount = 0;
        using (var host = BuildWebHost<DoubleAuthConnectionHandler>("/auth",
            options => authCount += options.AuthorizationData.Count))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/auth/negotiate", endpoint.DisplayName);
                    Assert.Equal(2, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                },
                endpoint =>
                {
                    Assert.Equal("/auth", endpoint.DisplayName);
                    Assert.Equal(2, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapConnectionHandlerFindsAttributesFromEndPointAndOptions()
    {
        var authCount = 0;
        using (var host = BuildWebHost<AuthConnectionHandler>("/auth",
            options =>
            {
                authCount += options.AuthorizationData.Count;
                options.AuthorizationData.Add(new AuthorizeAttribute());
            }))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/auth/negotiate", endpoint.DisplayName);
                    Assert.Equal(2, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                },
                endpoint =>
                {
                    Assert.Equal("/auth", endpoint.DisplayName);
                    Assert.Equal(2, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapConnectionHandlerEndPointRoutingFindsAttributesOnHub()
    {
        var authCount = 0;
        using (var host = BuildWebHost<AuthConnectionHandler>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
        }))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapConnectionHandlerEndPointRoutingFindsAttributesFromOptions()
    {
        var authCount = 0;
        using (var host = BuildWebHost<AuthConnectionHandler>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
            options.AuthorizationData.Add(new AuthorizeAttribute());
        }))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Equal(2, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(2, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                });
        }

        Assert.Equal(0, authCount);
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

        using (var host = BuildWebHost(ConfigureRoutes))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Collection(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                        auth => { },
                        auth =>
                        {
                            Assert.Equal("Foo", auth?.Policy);
                        });
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Collection(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                        auth => { },
                        auth =>
                        {
                            Assert.Equal("Foo", auth?.Policy);
                        });
                });
        }
    }

    [Fact]
    public void MapConnectionHandlerEndPointRoutingAppliesNegotiateMetadata()
    {
        void ConfigureRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapConnectionHandler<AuthConnectionHandler>("/path");
        }

        using (var host = BuildWebHost(ConfigureRoutes))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    var metaData = endpoint.Metadata.GetMetadata<NegotiateMetadata>();
                    Assert.NotNull(metaData);
                    var optionsMetaData = endpoint.Metadata.GetMetadata<HttpConnectionDispatcherOptions>();
                    Assert.NotNull(optionsMetaData);
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Null(endpoint.Metadata.GetMetadata<NegotiateMetadata>());
                });
        }
    }

    [Fact]
    public void MapConnectionHandlerNegotiateMetadataContainsOptions()
    {
        void ConfigureRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapConnectionHandler<AuthConnectionHandler>("/path", options =>
            {
                options.Transports = HttpTransportType.ServerSentEvents;
                options.ApplicationMaxBufferSize = 2;
                options.CloseOnAuthenticationExpiration = true;
            });
        }

        using (var host = BuildWebHost(ConfigureRoutes))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    var metaData = endpoint.Metadata.GetMetadata<NegotiateMetadata>();
                    Assert.NotNull(metaData);
                    var optionsMetaData = endpoint.Metadata.GetMetadata<HttpConnectionDispatcherOptions>();
                    Assert.NotNull(optionsMetaData);
                    Assert.Equal(HttpTransportType.ServerSentEvents, optionsMetaData.Transports);
                    Assert.Equal(2, optionsMetaData.ApplicationMaxBufferSize);
                    Assert.True(optionsMetaData.CloseOnAuthenticationExpiration);
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Null(endpoint.Metadata.GetMetadata<NegotiateMetadata>());
                });
        }
    }

    [Fact]
    public void MapConnectionHandlerEndPointRoutingAppliesCorsMetadata()
    {
        void ConfigureRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapConnectionHandler<CorsConnectionHandler>("/path");
        }

        using (var host = BuildWebHost(ConfigureRoutes))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.NotNull(endpoint.Metadata.GetMetadata<IEnableCorsAttribute>());
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.NotNull(endpoint.Metadata.GetMetadata<IEnableCorsAttribute>());
                });
        }
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public async Task MapConnectionHandlerWithWebSocketSubProtocolSetsProtocol()
    {
        using var host = BuildWebHost<MyConnectionHandler>("/socket",
            options => options.WebSockets.SubProtocolSelector = subprotocols =>
            {
                Assert.Equal(new[] { "protocol1", "protocol2" }, subprotocols.ToArray());
                return "protocol1";
            });

        await host.StartAsync();

        var feature = host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature.Addresses.First().Replace("http", "ws") + "/socket";

        var client = new ClientWebSocket();
        client.Options.AddSubProtocol("protocol1");
        client.Options.AddSubProtocol("protocol2");
        await client.ConnectAsync(new Uri(address), CancellationToken.None);
        Assert.Equal("protocol1", client.SubProtocol);
        await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).DefaultTimeout();
        var result = await client.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None).DefaultTimeout();
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
    }

    [Fact]
    public void MapConnectionHandlerAddsDisableRequestTimeoutMetadata()
    {
        using var host = BuildWebHost<MyConnectionHandler>("/test", o => { });
        host.Start();

        var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
        // We register 2 endpoints (/negotiate and /)
        Assert.Collection(dataSource.Endpoints,
            endpoint =>
            {
                Assert.Equal("/test/negotiate", endpoint.DisplayName);
                Assert.Empty(endpoint.Metadata.GetOrderedMetadata<DisableRequestTimeoutAttribute>());
            },
            endpoint =>
            {
                Assert.Equal("/test", endpoint.DisplayName);
                Assert.Single(endpoint.Metadata.GetOrderedMetadata<DisableRequestTimeoutAttribute>());
            });
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

    [EnableCors]
    private class CorsConnectionHandler : ConnectionHandler
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            throw new NotImplementedException();
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

    private class TestRequirement : IAuthorizationRequirement
    {
    }

    private IHost BuildWebHost(Action<IEndpointRouteBuilder> configure)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
                .UseUrls("http://127.0.0.1:0");
            })
            .Build();
    }

    private IHost BuildWebHost<TConnectionHandler>(string path, Action<HttpConnectionDispatcherOptions> configureOptions, Action<IEndpointConventionBuilder> configureEndpoints = null) where TConnectionHandler : ConnectionHandler
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseUrls("http://127.0.0.1:0")
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddConnections();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(routes =>
                    {
                        var builder = routes.MapConnectionHandler<TConnectionHandler>(path, configureOptions);
                        if (configureEndpoints != null)
                        {
                            configureEndpoints(builder);
                        }
                    });
                })
                .ConfigureLogging(factory =>
                {
                    factory.AddXunit(_output, LogLevel.Trace);
                });
            })
            .Build();
    }
}
