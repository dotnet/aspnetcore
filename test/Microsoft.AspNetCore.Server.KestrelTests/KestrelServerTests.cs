// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
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
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };

            using (var server = CreateServer(new KestrelServerOptions() { ThreadCount = threadCount }, testLogger))
            {
                var exception = Assert.Throws<ArgumentOutOfRangeException>(() => StartDummyApplication(server));

                Assert.Equal("threadCount", exception.ParamName);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Fact]
        public void StartWithInvalidAddressThrows()
        {
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };

            using (var server = CreateServer(new KestrelServerOptions(), testLogger))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("http:/asdf");

                var exception = Assert.Throws<FormatException>(() => StartDummyApplication(server));

                Assert.Contains("Invalid URL", exception.Message);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Theory]
        [InlineData("http://localhost:5000")]
        [InlineData("The value of the string shouldn't matter.")]
        [InlineData(null)]
        public void StartWarnsWhenIgnoringIServerAddressesFeature(string ignoredAddress)
        {
            var testLogger = new TestApplicationErrorLogger();
            var kestrelOptions = new KestrelServerOptions();

            // Directly configuring an endpoint using Listen causes the IServerAddressesFeature to be ignored.
            kestrelOptions.Listen(IPAddress.Loopback, 0);

            using (var server = CreateServer(kestrelOptions, testLogger))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add(ignoredAddress);
                StartDummyApplication(server);

                var warning = testLogger.Messages.Single(log => log.LogLevel == LogLevel.Warning);
                Assert.True(warning.Message.Contains("Overriding"));
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(int.MaxValue - 1, int.MaxValue)]
        public void StartWithMaxRequestBufferSizeLessThanMaxRequestLineSizeThrows(long maxRequestBufferSize, int maxRequestLineSize)
        {
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };
            var options = new KestrelServerOptions();
            options.Limits.MaxRequestBufferSize = maxRequestBufferSize;
            options.Limits.MaxRequestLineSize = maxRequestLineSize;

            using (var server = CreateServer(options, testLogger))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

                Assert.Equal(
                    $"Maximum request buffer size ({maxRequestBufferSize}) must be greater than or equal to maximum request line size ({maxRequestLineSize}).",
                    exception.Message);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(int.MaxValue - 1, int.MaxValue)]
        public void StartWithMaxRequestBufferSizeLessThanMaxRequestHeadersTotalSizeThrows(long maxRequestBufferSize, int maxRequestHeadersTotalSize)
        {
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };
            var options = new KestrelServerOptions();
            options.Limits.MaxRequestBufferSize = maxRequestBufferSize;
            options.Limits.MaxRequestLineSize = (int)maxRequestBufferSize;
            options.Limits.MaxRequestHeadersTotalSize = maxRequestHeadersTotalSize;

            using (var server = CreateServer(options, testLogger))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

                Assert.Equal(
                    $"Maximum request buffer size ({maxRequestBufferSize}) must be greater than or equal to maximum request headers size ({maxRequestHeadersTotalSize}).",
                    exception.Message);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Fact]
        public void LoggerCategoryNameIsKestrelServerNamespace()
        {
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            new KestrelServer(Options.Create<KestrelServerOptions>(null), new LifetimeNotImplemented(), mockLoggerFactory.Object);
            mockLoggerFactory.Verify(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"));
        }

        private static KestrelServer CreateServer(KestrelServerOptions options, ILogger testLogger)
        {
            var lifetime = new LifetimeNotImplemented();

            return new KestrelServer(Options.Create(options), lifetime, new KestrelTestLoggerFactory(testLogger));
        }

        private static void StartDummyApplication(IServer server)
        {
            server.Start(new DummyApplication(context => TaskCache.CompletedTask));
        }
    }
}
