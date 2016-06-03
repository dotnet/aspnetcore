// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            var server = CreateServer(new KestrelServerOptions());
            server.Features.Get<IServerAddressesFeature>().Addresses.Add("http:/asdf");

            var exception = Assert.Throws<FormatException>(() => StartDummyApplication(server));

            Assert.Contains("Invalid URL", exception.Message);
        }

        [Fact]
        public void StartWithEmptyAddressesThrows()
        {
            var server = CreateServer(new KestrelServerOptions());

            var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

            Assert.Equal("No recognized listening addresses were configured.", exception.Message);
        }

        private static KestrelServer CreateServer(KestrelServerOptions options)
        {
            var lifetime = new LifetimeNotImplemented();
            var logger = new LoggerFactory();

            return new KestrelServer(Options.Create(options), lifetime, logger);
        }

        private static void StartDummyApplication(IServer server)
        {
            server.Start(new DummyApplication(context => TaskUtilities.CompletedTask));
        }
    }
}
