// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class MapSignalRTests
{
    [Fact]
    public void MapSignalRFailsForInvalidHub()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
        {
            using (var host = BuildWebHost(routes => routes.MapHub<InvalidHub>("/overloads")))
            {
                host.Start();
            }
        });

        Assert.Equal("Duplicate definitions of 'OverloadedMethod'. Overloading is not supported.", ex.Message);
    }

    [Fact]
    public void NotAddingSignalRServiceThrows()
    {
        var executedConfigure = false;
        var builder = new HostBuilder();

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder
            .UseKestrel()
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Configure(app =>
            {
                executedConfigure = true;

                var ex = Assert.Throws<InvalidOperationException>(() =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<AuthHub>("/overloads");
                    });
                });

                Assert.Equal("Unable to find the required services. Please add all the required services by calling " +
                             "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.", ex.Message);
            })
            .UseUrls("http://127.0.0.1:0");
        });

        using (var host = builder.Build())
        {
            host.Start();
        }

        Assert.True(executedConfigure);
    }

    [Fact]
    public void MapHubFindsAuthAttributeOnHub()
    {
        var authCount = 0;
        using (var host = BuildWebHost(routes => routes.MapHub<AuthHub>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
        })))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapHubFindsMetadataPolicyOnHub()
    {
        var authCount = 0;
        var policy1 = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        var req = new TestRequirement();
        using (var host = BuildWebHost(routes => routes.MapHub<AuthHub>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
        })
        .RequireAuthorization(policy1)
        .RequireAuthorization(policy => policy.AddRequirements(req))))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                    var policies = endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>();
                    Assert.Equal(2, policies.Count);
                    Assert.Equal(policy1, policies[0]);
                    Assert.Equal(1, policies[1].Requirements.Count);
                    Assert.Equal(req, policies[1].Requirements.First());
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                    var policies = endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>();
                    Assert.Equal(2, policies.Count);
                    Assert.Equal(policy1, policies[0]);
                    Assert.Equal(1, policies[1].Requirements.Count);
                    Assert.Equal(req, policies[1].Requirements.First());
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapHubFindsAuthAttributeOnInheritedHub()
    {
        var authCount = 0;
        using (var host = BuildWebHost(routes => routes.MapHub<InheritedAuthHub>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
        })))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapHubFindsMultipleAuthAttributesOnDoubleAuthHub()
    {
        var authCount = 0;
        using (var host = BuildWebHost(routes => routes.MapHub<DoubleAuthHub>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
        })))
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
    public void MapHubEndPointRoutingFindsAttributesOnHub()
    {
        var authCount = 0;
        using (var host = BuildWebHost(routes => routes.MapHub<AuthHub>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
        })))
        {
            host.Start();

            var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
            // We register 2 endpoints (/negotiate and /)
            Assert.Collection(dataSource.Endpoints,
                endpoint =>
                {
                    Assert.Equal("/path/negotiate", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(1, endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count);
                });
        }

        Assert.Equal(0, authCount);
    }

    [Fact]
    public void MapHubEndPointRoutingFindsAttributesOnHubAndFromOptions()
    {
        var authCount = 0;
        HttpConnectionDispatcherOptions configuredOptions = null;
        using (var host = BuildWebHost(routes => routes.MapHub<AuthHub>("/path", options =>
        {
            authCount += options.AuthorizationData.Count;
            options.AuthorizationData.Add(new AuthorizeAttribute());
            configuredOptions = options;
        })))
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
    public void MapHubEndPointRoutingAppliesAttributesBeforeConventions()
    {
        void ConfigureRoutes(IEndpointRouteBuilder endpoints)
        {
            // This "Foo" policy should override the default auth attribute
            endpoints.MapHub<AuthHub>("/path")
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
    public void MapHubEndPointRoutingAppliesHubMetadata()
    {
        void ConfigureRoutes(IEndpointRouteBuilder endpoints)
        {
            // This "Foo" policy should override the default auth attribute
            endpoints.MapHub<AuthHub>("/path");
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
                    Assert.Equal(typeof(AuthHub), endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType);
                    Assert.NotNull(endpoint.Metadata.GetMetadata<NegotiateMetadata>());
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(typeof(AuthHub), endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType);
                    Assert.Null(endpoint.Metadata.GetMetadata<NegotiateMetadata>());
                });
        }
    }

    [Fact]
    public void MapHubAppliesHubMetadata()
    {
        void ConfigureRoutes(IEndpointRouteBuilder routes)
        {
            // This "Foo" policy should override the default auth attribute
            routes.MapHub<AuthHub>("/path");
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
                    Assert.Equal(typeof(AuthHub), endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType);
                    Assert.NotNull(endpoint.Metadata.GetMetadata<NegotiateMetadata>());
                },
                endpoint =>
                {
                    Assert.Equal("/path", endpoint.DisplayName);
                    Assert.Equal(typeof(AuthHub), endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType);
                    Assert.Null(endpoint.Metadata.GetMetadata<NegotiateMetadata>());
                });
        }
    }

    private class InvalidHub : Hub
    {
        public void OverloadedMethod(int num)
        {
        }

        public void OverloadedMethod(string message)
        {
        }
    }

    [Authorize]
    private class DoubleAuthHub : AuthHub
    {
    }

    private class InheritedAuthHub : AuthHub
    {
    }

    [Authorize]
    private class AuthHub : Hub
    {
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
                    services.AddSignalR();
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
}
