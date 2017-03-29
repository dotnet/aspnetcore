// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Server.Kestrel;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class KestrelServerOptionsTests
    {
#pragma warning disable CS0618
        [Fact]
        public void MaxRequestBufferSizeIsMarkedObsolete()
        {
            Assert.NotNull(typeof(KestrelServerOptions)
                .GetProperty(nameof(KestrelServerOptions.MaxRequestBufferSize))
                .GetCustomAttributes(false)
                .OfType<ObsoleteAttribute>()
                .SingleOrDefault());
        }

        [Fact]
        public void MaxRequestBufferSizeGetsLimitsProperty()
        {
            var o = new KestrelServerOptions();
            o.Limits.MaxRequestBufferSize = 42;
            Assert.Equal(42, o.MaxRequestBufferSize);
        }

        [Fact]
        public void MaxRequestBufferSizeSetsLimitsProperty()
        {
            var o = new KestrelServerOptions();
            o.MaxRequestBufferSize = 42;
            Assert.Equal(42, o.Limits.MaxRequestBufferSize);
        }
#pragma warning restore CS0612

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
    }
}