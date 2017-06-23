using System;
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
                var builder = new WebHostBuilder()
                        .UseKestrel()
                        .ConfigureServices(services =>
                        {
                            services.AddSignalR();
                        })
                        .Configure(app =>
                        {
                            app.UseSignalR(options => options.MapHub<InvalidHub>("overloads"));
                        })
                        .Build();
            });

            Assert.Equal("Duplicate definitions of 'OverloadedMethod'. Overloading is not supported.", ex.Message);
        }

        [Fact]
        public void MapHubFindsAuthAttributeOnHub()
        {
            var authCount = 0;
            var builder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .Configure(app =>
                {
                    app.UseSignalR(options => options.MapHub<AuthHub>("path", httpSocketOptions =>
                    {
                        authCount += httpSocketOptions.AuthorizationData.Count;
                    }));
                })
                .Build();

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapHubFindsAuthAttributeOnInheritedHub()
        {
            var authCount = 0;
            var builder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .Configure(app =>
                {
                    app.UseSignalR(options => options.MapHub<InheritedAuthHub>("path", httpSocketOptions =>
                    {
                        authCount += httpSocketOptions.AuthorizationData.Count;
                    }));
                })
                .Build();

            Assert.Equal(1, authCount);
        }

        [Fact]
        public void MapHubFindsMultipleAuthAttributesOnDoubleAuthHub()
        {
            var authCount = 0;
            var builder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .Configure(app =>
                {
                    app.UseSignalR(options => options.MapHub<DoubleAuthHub>("path", httpSocketOptions =>
                    {
                        authCount += httpSocketOptions.AuthorizationData.Count;
                    }));
                })
                .Build();

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
    }
}
