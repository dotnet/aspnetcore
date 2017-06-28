// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
