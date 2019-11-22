// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ListenOptionsTests
    {
        [Fact]
        public void ProtocolsDefault()
        {
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            Assert.Equal(HttpProtocols.Http1AndHttp2, listenOptions.Protocols);
        }

        [Fact]
        public void LocalHostListenOptionsClonesConnectionMiddleware()
        {
            var localhostListenOptions = new LocalhostListenOptions(1004);
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            localhostListenOptions.KestrelServerOptions = new KestrelServerOptions
            {
                ApplicationServices = serviceProvider
            };
            var middlewareRan = false;
            localhostListenOptions.Use(next =>
            {
                middlewareRan = true;
                return context => Task.CompletedTask;
            });

            var clone = localhostListenOptions.Clone(IPAddress.IPv6Loopback);
            var app = clone.Build();

            // Execute the delegate
            app(null);

            Assert.True(middlewareRan);
            Assert.NotNull(clone.KestrelServerOptions);
            Assert.NotNull(serviceProvider);
            Assert.Same(serviceProvider, clone.ApplicationServices);
        }
    }
}
