using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
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
            var builder = new WebHostBuilder();

            builder
                .UseKestrel()
                .Configure(app =>
                {
                    executedConfigure = true;

                    var ex = Assert.Throws<InvalidOperationException>(() =>
                    {
                        app.UseSignalR(routes =>
                        {
                            routes.MapHub<AuthHub>("/overloads");
                        });
                    });

                    Assert.Equal("Unable to find the required services. Please add all the required services by calling " +
                                 "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.", ex.Message);
                })
                .UseUrls("http://127.0.0.1:0");

            using (var host = builder.Build())
            {
                host.Start();
            }

            Assert.True(executedConfigure);
        }

        [Fact]
        public void NotAddingSignalRServiceThrowsWhenUsingEndpointRouting()
        {
            var executedConfigure = false;
            var builder = new WebHostBuilder();

            builder
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
            }

            Assert.Equal(1, authCount);
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
            }

            Assert.Equal(1, authCount);
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
            }

            Assert.Equal(2, authCount);
        }

        [Fact]
        public void MapHubEndPointRoutingFindsAttributesOnHub()
        {
            var authCount = 0;
            using (var host = BuildWebHostWithEndPointRouting(routes => routes.MapHub<AuthHub>("/path", options =>
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
        public void MapHubEndPointRoutingAppliesAttributesBeforeConventions()
        {
            void ConfigureRoutes(IEndpointRouteBuilder endpoints)
            {
                // This "Foo" policy should override the default auth attribute
                endpoints.MapHub<AuthHub>("/path")
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
        public void MapHubEndPointRoutingAppliesHubMetadata()
        {
            void ConfigureRoutes(IEndpointRouteBuilder endpoints)
            {
                // This "Foo" policy should override the default auth attribute
                endpoints.MapHub<AuthHub>("/path");
            }

            using (var host = BuildWebHostWithEndPointRouting(ConfigureRoutes))
            {
                host.Start();

                var dataSource = host.Services.GetRequiredService<EndpointDataSource>();
                // We register 2 endpoints (/negotiate and /)
                Assert.Equal(2, dataSource.Endpoints.Count);
                Assert.Equal(typeof(AuthHub), dataSource.Endpoints[0].Metadata.GetMetadata<HubMetadata>()?.HubType);
                Assert.Equal(typeof(AuthHub), dataSource.Endpoints[1].Metadata.GetMetadata<HubMetadata>()?.HubType);
                Assert.NotNull(dataSource.Endpoints[0].Metadata.GetMetadata<NegotiateMetadata>());
                Assert.Null(dataSource.Endpoints[1].Metadata.GetMetadata<NegotiateMetadata>());
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

        private IWebHost BuildWebHostWithEndPointRouting(Action<IEndpointRouteBuilder> configure)
        {
            return new WebHostBuilder()
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
                .UseUrls("http://127.0.0.1:0")
                .Build();
        }

        private IWebHost BuildWebHost(Action<HubRouteBuilder> configure)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .Configure(app =>
                {
                    app.UseSignalR(options => configure(options));
                })
                .UseUrls("http://127.0.0.1:0")
                .Build();
        }
    }
}
