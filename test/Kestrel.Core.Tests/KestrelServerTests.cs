// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
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
                Assert.Contains("Overriding", warning.Message);
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

        [Fact]
        public async Task StopAsyncCallsCompleteWhenFirstCallCompletes()
        {
            var options = new KestrelServerOptions
            {
                ListenOptions =
                {
                    new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                }
            };

            var unbind = new SemaphoreSlim(0);
            var stop = new SemaphoreSlim(0);

            var mockTransport = new Mock<ITransport>();
            mockTransport
                .Setup(transport => transport.BindAsync())
                .Returns(Task.CompletedTask);
            mockTransport
                .Setup(transport => transport.UnbindAsync())
                .Returns(async () => await unbind.WaitAsync());
            mockTransport
                .Setup(transport => transport.StopAsync())
                .Returns(async () => await stop.WaitAsync());

            var mockTransportFactory = new Mock<ITransportFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.Create(It.IsAny<IEndPointInformation>(), It.IsAny<IConnectionHandler>()))
                .Returns(mockTransport.Object);

            var server = new KestrelServer(Options.Create(options), mockTransportFactory.Object, Mock.Of<LoggerFactory>());
            await server.StartAsync(new DummyApplication(), CancellationToken.None);

            var stopTask1 = server.StopAsync(default(CancellationToken));
            var stopTask2 = server.StopAsync(default(CancellationToken));
            var stopTask3 = server.StopAsync(default(CancellationToken));

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            unbind.Release();
            stop.Release();

            await Task.WhenAll(new[] { stopTask1, stopTask2, stopTask3 }).TimeoutAfter(TimeSpan.FromSeconds(10));

            mockTransport.Verify(transport => transport.UnbindAsync(), Times.Once);
            mockTransport.Verify(transport => transport.StopAsync(), Times.Once);
        }

        [Fact]
        public async Task StopAsyncCallsCompleteWithThrownException()
        {
            var options = new KestrelServerOptions
            {
                ListenOptions =
                {
                    new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                }
            };

            var unbind = new SemaphoreSlim(0);
            var unbindException = new InvalidOperationException();

            var mockTransport = new Mock<ITransport>();
            mockTransport
                .Setup(transport => transport.BindAsync())
                .Returns(Task.CompletedTask);
            mockTransport
                .Setup(transport => transport.UnbindAsync())
                .Returns(async () =>
                {
                    await unbind.WaitAsync();
                    throw unbindException;
                });
            mockTransport
                .Setup(transport => transport.StopAsync())
                .Returns(Task.CompletedTask);

            var mockTransportFactory = new Mock<ITransportFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.Create(It.IsAny<IEndPointInformation>(), It.IsAny<IConnectionHandler>()))
                .Returns(mockTransport.Object);

            var server = new KestrelServer(Options.Create(options), mockTransportFactory.Object, Mock.Of<LoggerFactory>());
            await server.StartAsync(new DummyApplication(), CancellationToken.None);

            var stopTask1 = server.StopAsync(default(CancellationToken));
            var stopTask2 = server.StopAsync(default(CancellationToken));
            var stopTask3 = server.StopAsync(default(CancellationToken));

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            unbind.Release();

            var timeout = TimeSpan.FromSeconds(10);
            Assert.Same(unbindException, await Assert.ThrowsAsync<InvalidOperationException>(() => stopTask1.TimeoutAfter(timeout)));
            Assert.Same(unbindException, await Assert.ThrowsAsync<InvalidOperationException>(() => stopTask2.TimeoutAfter(timeout)));
            Assert.Same(unbindException, await Assert.ThrowsAsync<InvalidOperationException>(() => stopTask3.TimeoutAfter(timeout)));

            mockTransport.Verify(transport => transport.UnbindAsync(), Times.Once);
        }

        private static KestrelServer CreateServer(KestrelServerOptions options, ILogger testLogger)
        {
            return new KestrelServer(Options.Create(options), new MockTransportFactory(), new LoggerFactory(new[] { new KestrelTestLoggerProvider(testLogger) }));
        }

        private static void StartDummyApplication(IServer server)
        {
            server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None).GetAwaiter().GetResult();
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
