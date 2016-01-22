// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Configuration;
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
            var server = CreateServer(configuration =>
                new KestrelServerInformation(configuration)
                {
                    ThreadCount = threadCount
                });

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => StartDummyApplication(server));

            Assert.Equal("threadCount", exception.ParamName);
        }

        [Fact]
        public void StartWithInvalidAddressThrows()
        {
            var server = CreateServer(configuration =>
                new KestrelServerInformation(configuration)
                {
                    Addresses = {"http:/asdf"}
                });

            var exception = Assert.Throws<FormatException>(() => StartDummyApplication(server));

            Assert.Contains("Unrecognized listening address", exception.Message);
        }

        [Fact]
        public void StartWithEmptyAddressesThrows()
        {
            var server = CreateServer(configuration =>
            {
                var information = new KestrelServerInformation(configuration);

                information.Addresses.Clear();

                return information;
            });

            var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

            Assert.Equal("No recognized listening addresses were configured.", exception.Message);
        }

        private static KestrelServer CreateServer(Func<IConfiguration, IKestrelServerInformation> serverInformationFactory)
        {
            var configuration = new ConfigurationBuilder().Build();
            var information = serverInformationFactory(configuration);

            var features = new FeatureCollection();
            features.Set(information);

            var lifetime = new LifetimeNotImplemented();
            var logger = new TestKestrelTrace.TestLogger();

            return new KestrelServer(features, lifetime, logger);
        }

        private static void StartDummyApplication(IServer server)
        {
            server.Start(new DummyApplication(context => TaskUtilities.CompletedTask));
        }
    }
}
