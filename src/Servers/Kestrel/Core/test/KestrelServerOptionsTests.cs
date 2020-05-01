// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
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
    }
}
