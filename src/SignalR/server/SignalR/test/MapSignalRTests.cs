using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

                    var ex = Assert.Throws<InvalidOperationException>(() => {
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
