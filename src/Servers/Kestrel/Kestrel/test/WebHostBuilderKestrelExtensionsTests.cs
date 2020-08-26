// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
    public class WebHostBuilderKestrelExtensionsTests
    {
        [Fact]
        public void ApplicationServicesNotNullAfterUseKestrelWithoutOptions()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .Configure(app => { });

            hostBuilder.ConfigureServices(services =>
            {
                services.Configure<KestrelServerOptions>(options =>
                {
                    // Assert
                    Assert.NotNull(options.ApplicationServices);
                });
            });

            // Act
            hostBuilder.Build();
        }

        [Fact]
        public void ApplicationServicesNotNullDuringUseKestrelWithOptions()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    // Assert
                    Assert.NotNull(options.ApplicationServices);
                })
                .Configure(app => { });

            // Act
            hostBuilder.Build();
        }

        [Fact]
        public void SocketTransportIsTheDefault()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .Configure(app => { });

            Assert.IsType<SocketTransportFactory>(hostBuilder.Build().Services.GetService<IConnectionListenerFactory>());
        }

        [Fact]
        public void LibuvTransportCanBeManuallySelectedIndependentOfOrder()
        {
#pragma warning disable CS0618
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseLibuv()
                .Configure(app => { });
#pragma warning restore CS0618

            Assert.IsType<LibuvTransportFactory>(hostBuilder.Build().Services.GetService<IConnectionListenerFactory>());

#pragma warning disable CS0618
            var hostBuilderReversed = new WebHostBuilder()
                .UseLibuv()
                .UseKestrel()
                .Configure(app => { });
#pragma warning restore CS0618

            Assert.IsType<LibuvTransportFactory>(hostBuilderReversed.Build().Services.GetService<IConnectionListenerFactory>());
        }

        [Fact]
        public void SocketsTransportCanBeManuallySelectedIndependentOfOrder()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseSockets()
                .Configure(app => { });

            Assert.IsType<SocketTransportFactory>(hostBuilder.Build().Services.GetService<IConnectionListenerFactory>());

            var hostBuilderReversed = new WebHostBuilder()
                .UseSockets()
                .UseKestrel()
                .Configure(app => { });

            Assert.IsType<SocketTransportFactory>(hostBuilderReversed.Build().Services.GetService<IConnectionListenerFactory>());
        }

        [Fact]
        public void ServerIsKestrelServerImpl()
        {
            var hostBuilder = new WebHostBuilder()
                .UseSockets()
                .UseKestrel()
                .Configure(app => { });

            Assert.IsType<KestrelServerImpl>(hostBuilder.Build().Services.GetService<IServer>());
        }
    }
}
