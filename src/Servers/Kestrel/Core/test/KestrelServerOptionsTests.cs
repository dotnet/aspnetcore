// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class KestrelServerOptionsTests
    {
        [Fact]
        public void AllowSynchronousIODefaultsToFalse()
        {
            var options = new KestrelServerOptions();

            Assert.False(options.AllowSynchronousIO);
        }

        [Fact]
        public void ConfigureEndpointDefaultsAppliesToNewEndpoints()
        {
            var options = new KestrelServerOptions();
            options.ListenLocalhost(5000);

            Assert.Equal(HttpProtocols.Http1AndHttp2, options.CodeBackedListenOptions[0].Protocols);

            options.ConfigureEndpointDefaults(opt =>
            {
                opt.Protocols = HttpProtocols.Http1;
            });

            options.Listen(new IPEndPoint(IPAddress.Loopback, 5000), opt =>
            {
                // ConfigureEndpointDefaults runs before this callback
                Assert.Equal(HttpProtocols.Http1, opt.Protocols);
            });
            Assert.Equal(HttpProtocols.Http1, options.CodeBackedListenOptions[1].Protocols);

            options.ListenLocalhost(5000, opt =>
            {
                Assert.Equal(HttpProtocols.Http1, opt.Protocols);
                opt.Protocols = HttpProtocols.Http2; // Can be overriden
            });
            Assert.Equal(HttpProtocols.Http2, options.CodeBackedListenOptions[2].Protocols);

            options.ListenAnyIP(5000, opt =>
            {
                opt.Protocols = HttpProtocols.Http2;
            });
            Assert.Equal(HttpProtocols.Http2, options.CodeBackedListenOptions[3].Protocols);
        }

        [Fact]
        public void ConfigureThrowsInvalidOperationExceptionIfApplicationServicesIsNotSet()
        {
            var options = new KestrelServerOptions();
            Assert.Throws<InvalidOperationException>(() => options.Configure());
        }

        [Fact]
        public void ConfigureThrowsInvalidOperationExceptionIfApplicationServicesDoesntHaveRequiredServices()
        {
            var options = new KestrelServerOptions
            {
                ApplicationServices = new ServiceCollection().BuildServiceProvider()
            };

            Assert.Throws<InvalidOperationException>(() => options.Configure());
        }

        [Fact]
        public void CanCallListenAfterConfigure()
        {
            var options = new KestrelServerOptions();

            // Ensure configure doesn't throw because of missing services.
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(Mock.Of<IHostEnvironment>());
            serviceCollection.AddSingleton(Mock.Of<ILogger<KestrelServer>>());
            serviceCollection.AddSingleton(Mock.Of<ILogger<HttpsConnectionMiddleware>>());
            options.ApplicationServices = serviceCollection.BuildServiceProvider();

            options.Configure();

            // This is a regression test to verify the Listen* methods don't throw a NullReferenceException if called after Configure().
            // https://github.com/dotnet/aspnetcore/issues/21423
            options.ListenLocalhost(5000);
        }

        [Fact]
        public void SettingRequestHeaderEncodingSelecterToNullThrowsArgumentNullException()
        {
            var options = new KestrelServerOptions();

            var ex = Assert.Throws<ArgumentNullException>(() => options.RequestHeaderEncodingSelector = null);
            Assert.Equal("value", ex.ParamName);
        }
    }
}
