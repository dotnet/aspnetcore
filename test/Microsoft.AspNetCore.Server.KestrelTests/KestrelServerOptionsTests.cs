// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Server.Kestrel;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class KestrelServerInformationTests
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
        public void SetThreadCountUsingProcessorCount()
        {
            // Ideally we'd mock Environment.ProcessorCount to test edge cases.
            var expected = Clamp(Environment.ProcessorCount >> 1, 1, 16);

            var information = new KestrelServerOptions();

            Assert.Equal(expected, information.ThreadCount);
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}