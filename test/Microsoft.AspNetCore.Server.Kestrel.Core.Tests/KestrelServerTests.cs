// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class KestrelServerTests
    {
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

        [Fact]
        public void StartWithHttpsAddressThrows()
        {
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };

            using (var server = CreateServer(new KestrelServerOptions(), testLogger))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("https://127.0.0.1:0");

                var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

                Assert.Equal(
                    $"HTTPS endpoints can only be configured using {nameof(KestrelServerOptions)}.{nameof(KestrelServerOptions.Listen)}().",
                    exception.Message);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Fact]
        public void StartWithPathBaseInAddressThrows()
        {
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };

            using (var server = CreateServer(new KestrelServerOptions(), testLogger))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("http://127.0.0.1:0/base");

                var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

                Assert.Equal(
                    $"A path base can only be configured using {nameof(IApplicationBuilder)}.UsePathBase().",
                    exception.Message);
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
            var options = new KestrelServerOptions
            {
                Limits =
                {
                    MaxRequestBufferSize = maxRequestBufferSize,
                    MaxRequestLineSize = maxRequestLineSize
                }
            };

            using (var server = CreateServer(options, testLogger))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

                Assert.Equal(
                    CoreStrings.FormatMaxRequestBufferSmallerThanRequestLineBuffer(maxRequestBufferSize, maxRequestLineSize),
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
            var options = new KestrelServerOptions
            {
                Limits =
                {
                    MaxRequestBufferSize = maxRequestBufferSize,
                    MaxRequestLineSize = (int)maxRequestBufferSize,
                    MaxRequestHeadersTotalSize = maxRequestHeadersTotalSize
                }
            };

            using (var server = CreateServer(options, testLogger))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));

                Assert.Equal(
                    CoreStrings.FormatMaxRequestBufferSmallerThanRequestHeaderBuffer(maxRequestBufferSize, maxRequestHeadersTotalSize),
                    exception.Message);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Fact]
        public void LoggerCategoryNameIsKestrelServerNamespace()
        {
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            new KestrelServer(Options.Create<KestrelServerOptions>(null), Mock.Of<ITransportFactory>(), mockLoggerFactory.Object);
            mockLoggerFactory.Verify(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"));
        }

        [Fact]
        public void StartWithNoTransportFactoryThrows()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new KestrelServer(Options.Create<KestrelServerOptions>(null), null, Mock.Of<ILoggerFactory>()));

            Assert.Equal("transportFactory", exception.ParamName);
        }

        private static KestrelServer CreateServer(KestrelServerOptions options, ILogger testLogger)
        {
            return new KestrelServer(Options.Create(options), new MockTransportFactory(), new LoggerFactory(new [] { new KestrelTestLoggerProvider(testLogger)} ));
        }

        private static void StartDummyApplication(IServer server)
        {
            server.StartAsync(new DummyApplication(context => TaskCache.CompletedTask), CancellationToken.None).GetAwaiter().GetResult();
        }

        private class MockTransportFactory : ITransportFactory
        {
            public ITransport Create(IEndPointInformation endPointInformation, IConnectionHandler handler)
            {
                return Mock.Of<ITransport>();
            }
        }
    }
}
