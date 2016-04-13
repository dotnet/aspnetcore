// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class KestrelServerTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1337)]
        public void StartWithNonPositiveThreadCountThrows(int threadCount)
        {
            var server = CreateServer(new KestrelServerOptions() { ThreadCount = threadCount });

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => StartDummyApplication(server));

            Assert.Equal("threadCount", exception.ParamName);
        }

        [Fact]
        public void StartWithInvalidAddressThrows()
        {
            var addressesFeature = new ServerAddressesFeature();
            addressesFeature.Addresses.Add("http:/asdf");
            var server = CreateServer(new KestrelServerOptions(), addressesFeature);

            var exception = Assert.Throws<FormatException>(() => StartDummyApplication(server));

            Assert.Contains("Unrecognized listening address", exception.Message);
        }

        [Fact]
        public void StartWithEmptyAddressesThrows()
        {
            var server = CreateServer(new KestrelServerOptions(), new ServerAddressesFeature());

            var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

            Assert.Equal("No recognized listening addresses were configured.", exception.Message);
        }

        private static KestrelServer CreateServer(KestrelServerOptions options, IServerAddressesFeature addressesFeature = null)
        {
            var features = new FeatureCollection();
            if (addressesFeature != null)
            {
                features.Set(addressesFeature);
            }

            var lifetime = new LifetimeNotImplemented();
            var logger = new TestApplicationErrorLogger();

            return new KestrelServer(features, options, lifetime, logger);
        }

        private static void StartDummyApplication(IServer server)
        {
            server.Start(new DummyApplication(context => TaskUtilities.CompletedTask));
        }
    }
}
