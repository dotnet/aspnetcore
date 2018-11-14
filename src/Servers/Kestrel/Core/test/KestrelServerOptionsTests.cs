// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class KestrelServerOptionsTests
    {
        [Fact]
        public void NoDelayDefaultsToTrue()
        {
            var o1 = new KestrelServerOptions();
            o1.Listen(IPAddress.Loopback, 0);
            o1.Listen(IPAddress.Loopback, 0, d =>
            {
                d.NoDelay = false;
            });

            Assert.True(o1.ListenOptions[0].NoDelay);
            Assert.False(o1.ListenOptions[1].NoDelay);
        }

        [Fact]
        public void AllowSynchronousIODefaultsToTrue()
        {
            var options = new KestrelServerOptions();

            Assert.True(options.AllowSynchronousIO);
        }

        [Fact]
        public void ConfigureEndpointDefaultsAppliesToNewEndpoints()
        {
            var options = new KestrelServerOptions();
            options.ListenLocalhost(5000);

            Assert.True(options.ListenOptions[0].NoDelay);

            options.ConfigureEndpointDefaults(opt =>
            {
                opt.NoDelay = false;
            });

            options.Listen(new IPEndPoint(IPAddress.Loopback, 5000), opt =>
            {
                // ConfigureEndpointDefaults runs before this callback
                Assert.False(opt.NoDelay);
            });
            Assert.False(options.ListenOptions[1].NoDelay);

            options.ListenLocalhost(5000, opt =>
            {
                Assert.False(opt.NoDelay);
                opt.NoDelay = true; // Can be overriden
            });
            Assert.True(options.ListenOptions[2].NoDelay);


            options.ListenAnyIP(5000, opt =>
            {
                Assert.False(opt.NoDelay);
            });
            Assert.False(options.ListenOptions[3].NoDelay);
        }
    }
}