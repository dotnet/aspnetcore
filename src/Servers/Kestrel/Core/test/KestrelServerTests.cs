// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class KestrelServerTests
    {
        private KestrelServerOptions CreateServerOptions()
        {
            var serverOptions = new KestrelServerOptions();
            serverOptions.ApplicationServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            return serverOptions;
        }

        [Fact]
        public void StartWithInvalidAddressThrows()
        {
            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };

            using (var server = CreateServer(CreateServerOptions(), testLogger))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("http:/asdf");

                var exception = Assert.Throws<FormatException>(() => StartDummyApplication(server));

                Assert.Contains("Invalid url", exception.Message);
                Assert.Equal(1, testLogger.CriticalErrorsLogged);
            }
        }

        [Fact]
        public void StartWithHttpsAddressConfiguresHttpsEndpoints()
        {
            var options = CreateServerOptions();
            options.DefaultCertificate = TestResources.GetTestCertificate();
            using (var server = CreateServer(options))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("https://127.0.0.1:0");

                StartDummyApplication(server);

                Assert.True(server.Options.OptionsInUse.Any());
                Assert.True(server.Options.OptionsInUse[0].IsTls);
            }
        }

        [Fact]
        public void KestrelServerThrowsUsefulExceptionIfDefaultHttpsProviderNotAdded()
        {
            var options = CreateServerOptions();
            options.IsDevCertLoaded = true; // Prevent the system default from being loaded
            using (var server = CreateServer(options, throwOnCriticalErrors: false))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("https://127.0.0.1:0");

                var ex = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));
                Assert.Equal(CoreStrings.NoCertSpecifiedNoDevelopmentCertificateFound, ex.Message);
            }
        }

        [Fact]
        public void KestrelServerDoesNotThrowIfNoDefaultHttpsProviderButNoHttpUrls()
        {
            using (var server = CreateServer(CreateServerOptions()))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("http://127.0.0.1:0");

                StartDummyApplication(server);
            }
        }

        [Fact]
        public void KestrelServerDoesNotThrowIfNoDefaultHttpsProviderButManualListenOptions()
        {
            var serverOptions = CreateServerOptions();
            serverOptions.Listen(new IPEndPoint(IPAddress.Loopback, 0));

            using (var server = CreateServer(serverOptions))
            {
                server.Features.Get<IServerAddressesFeature>().Addresses.Add("https://127.0.0.1:0");

                StartDummyApplication(server);
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
            var mockLogger = new Mock<ILogger>();
            mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            new KestrelServer(Options.Create<KestrelServerOptions>(null), new List<IConnectionListenerFactory>() { new MockTransportFactory() }, mockLoggerFactory.Object);
            mockLoggerFactory.Verify(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"));
        }

        [Fact]
        public void ConstructorWithNullTransportFactoriesThrows()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new KestrelServer(
                    Options.Create<KestrelServerOptions>(null),
                    null,
                    new LoggerFactory(new[] { new KestrelTestLoggerProvider() })));

            Assert.Equal("transportFactories", exception.ParamName);
        }

        [Fact]
        public void ConstructorWithNoTransportFactoriesThrows()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new KestrelServer(
                    Options.Create<KestrelServerOptions>(null),
                    new List<IConnectionListenerFactory>(),
                    new LoggerFactory(new[] { new KestrelTestLoggerProvider() })));

            Assert.Equal(CoreStrings.TransportNotFound, exception.Message);
        }

        [Fact]
        public void StartWithMultipleTransportFactoriesDoesNotThrow()
        {
            using var server = new KestrelServer(
                Options.Create(CreateServerOptions()),
                new List<IConnectionListenerFactory>() { new ThrowingTransportFactory(), new MockTransportFactory() },
                new LoggerFactory(new[] { new KestrelTestLoggerProvider() }));

            StartDummyApplication(server);
        }

        [Fact]
        public void StartWithNullRequestHeaderEncodingSelectorThrows()
        {
            var kso = CreateServerOptions();
            kso.RequestHeaderEncodingSelector = null;

            var testLogger = new TestApplicationErrorLogger { ThrowOnCriticalErrors = false };

            using var server = CreateServer(kso, testLogger);

            var ex = Assert.Throws<InvalidOperationException>(() => StartDummyApplication(server));
            Assert.Contains(nameof(KestrelServerOptions.RequestHeaderEncodingSelector), ex.Message);

            Assert.Equal(1, testLogger.CriticalErrorsLogged);
        }

        [Fact]
        public async Task StopAsyncCallsCompleteWhenFirstCallCompletes()
        {
            var options = new KestrelServerOptions
            {
                CodeBackedListenOptions =
                {
                    new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                }
            };

            var unbind = new SemaphoreSlim(0);
            var stop = new SemaphoreSlim(0);

            var mockTransport = new Mock<IConnectionListener>();
            var mockTransportFactory = new Mock<IConnectionListenerFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.BindAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns<EndPoint, CancellationToken>((e, token) =>
                {
                    mockTransport
                        .Setup(transport => transport.AcceptAsync(It.IsAny<CancellationToken>()))
                        .Returns(new ValueTask<ConnectionContext>((ConnectionContext)null));
                    mockTransport
                        .Setup(transport => transport.UnbindAsync(It.IsAny<CancellationToken>()))
                        .Returns(() => new ValueTask(unbind.WaitAsync()));
                    mockTransport
                        .Setup(transport => transport.DisposeAsync())
                        .Returns(() => new ValueTask(stop.WaitAsync()));
                    mockTransport
                        .Setup(transport => transport.EndPoint).Returns(e);

                    return new ValueTask<IConnectionListener>(mockTransport.Object);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            var server = new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { mockTransportFactory.Object }, mockLoggerFactory.Object);
            await server.StartAsync(new DummyApplication(), CancellationToken.None);

            var stopTask1 = server.StopAsync(default);
            var stopTask2 = server.StopAsync(default);
            var stopTask3 = server.StopAsync(default);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            unbind.Release();
            stop.Release();

            await Task.WhenAll(new[] { stopTask1, stopTask2, stopTask3 }).DefaultTimeout();

            mockTransport.Verify(transport => transport.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StopAsyncCallsCompleteWithThrownException()
        {
            var options = new KestrelServerOptions
            {
                CodeBackedListenOptions =
                {
                    new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                }
            };

            var unbind = new SemaphoreSlim(0);
            var unbindException = new InvalidOperationException();

            var mockTransport = new Mock<IConnectionListener>();
            var mockTransportFactory = new Mock<IConnectionListenerFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.BindAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns<EndPoint, CancellationToken>((e, token) =>
                {
                    mockTransport
                        .Setup(transport => transport.AcceptAsync(It.IsAny<CancellationToken>()))
                        .Returns(new ValueTask<ConnectionContext>((ConnectionContext)null));
                    mockTransport
                        .Setup(transport => transport.UnbindAsync(It.IsAny<CancellationToken>()))
                        .Returns(async () =>
                        {
                            await unbind.WaitAsync();
                            throw unbindException;
                        });
                    mockTransport
                        .Setup(transport => transport.EndPoint).Returns(e);

                    return new ValueTask<IConnectionListener>(mockTransport.Object);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            var server = new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { mockTransportFactory.Object }, mockLoggerFactory.Object);
            await server.StartAsync(new DummyApplication(), CancellationToken.None);

            var stopTask1 = server.StopAsync(default);
            var stopTask2 = server.StopAsync(default);
            var stopTask3 = server.StopAsync(default);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            unbind.Release();

            var timeout = TestConstants.DefaultTimeout;
            Assert.Same(unbindException, await Assert.ThrowsAsync<InvalidOperationException>(() => stopTask1.TimeoutAfter(timeout)));
            Assert.Same(unbindException, await Assert.ThrowsAsync<InvalidOperationException>(() => stopTask2.TimeoutAfter(timeout)));
            Assert.Same(unbindException, await Assert.ThrowsAsync<InvalidOperationException>(() => stopTask3.TimeoutAfter(timeout)));

            mockTransport.Verify(transport => transport.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StopAsyncDispatchesSubsequentStopAsyncContinuations()
        {
            var options = new KestrelServerOptions
            {
                CodeBackedListenOptions =
                {
                    new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                }
            };

            var unbindTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockTransport = new Mock<IConnectionListener>();
            var mockTransportFactory = new Mock<IConnectionListenerFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.BindAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns<EndPoint, CancellationToken>((e, token) =>
                {
                    mockTransport
                        .Setup(transport => transport.AcceptAsync(It.IsAny<CancellationToken>()))
                        .Returns(new ValueTask<ConnectionContext>((ConnectionContext)null));
                    mockTransport
                        .Setup(transport => transport.UnbindAsync(It.IsAny<CancellationToken>()))
                        .Returns(new ValueTask(unbindTcs.Task));
                    mockTransport
                        .Setup(transport => transport.EndPoint).Returns(e);

                    return new ValueTask<IConnectionListener>(mockTransport.Object);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            var server = new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { mockTransportFactory.Object }, mockLoggerFactory.Object);
            await server.StartAsync(new DummyApplication(), default);

            var stopTask1 = server.StopAsync(default);
            var stopTask2 = server.StopAsync(default);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);

            var continuationTask = Task.Run(async () =>
            {
                await stopTask2;
                stopTask1.Wait();
            });

            unbindTcs.SetResult();

            // If stopTask2 is completed inline by the first call to StopAsync, stopTask1 will never complete.
            await stopTask1.DefaultTimeout();
            await stopTask2.DefaultTimeout();
            await continuationTask.DefaultTimeout();

            mockTransport.Verify(transport => transport.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void StartingServerInitializesHeartbeat()
        {
            var testContext = new TestServiceContext
            {
                ServerOptions =
                {
                    CodeBackedListenOptions =
                    {
                        new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                    }
                },
                DateHeaderValueManager = new DateHeaderValueManager()
            };

            testContext.Heartbeat = new Heartbeat(
                new IHeartbeatHandler[] { testContext.DateHeaderValueManager },
                testContext.MockSystemClock,
                DebuggerWrapper.Singleton,
                testContext.Log);

            using (var server = new KestrelServer(new List<IConnectionListenerFactory>() { new MockTransportFactory() }, testContext))
            {
                Assert.Null(testContext.DateHeaderValueManager.GetDateHeaderValues());

                // Ensure KestrelServer is started at a different time than when it was constructed, since we're
                // verifying the heartbeat is initialized during KestrelServer.StartAsync().
                testContext.MockSystemClock.UtcNow += TimeSpan.FromDays(1);

                StartDummyApplication(server);

                Assert.Equal(HeaderUtilities.FormatDate(testContext.MockSystemClock.UtcNow),
                             testContext.DateHeaderValueManager.GetDateHeaderValues().String);
            }
        }

        [Fact]
        public async Task ReloadsOnConfigurationChangeWhenOptedIn()
        {
            var currentConfig = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5001"),
            }).Build();

            Func<Task> changeCallback = null;
            TaskCompletionSource changeCallbackRegisteredTcs = null;

            var mockChangeToken = new Mock<IChangeToken>();
            mockChangeToken.Setup(t => t.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>())).Returns<Action<object>, object>((callback, state) =>
            {
                changeCallbackRegisteredTcs?.SetResult();

                changeCallback = () =>
                {
                    changeCallbackRegisteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    callback(state);
                    return changeCallbackRegisteredTcs.Task;
                };

                return Mock.Of<IDisposable>();
            });

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns<string>(name => currentConfig.GetSection(name));
            mockConfig.Setup(c => c.GetChildren()).Returns(() => currentConfig.GetChildren());
            mockConfig.Setup(c => c.GetReloadToken()).Returns(() => mockChangeToken.Object);

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(mockLoggerFactory.Object);
            serviceCollection.AddSingleton(Mock.Of<ILogger<KestrelServer>>());

            var options = new KestrelServerOptions
            {
                ApplicationServices = serviceCollection.BuildServiceProvider(),
            };

            options.Configure(mockConfig.Object, reloadOnChange: true);

            var mockTransports = new List<Mock<IConnectionListener>>();
            var mockTransportFactory = new Mock<IConnectionListenerFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.BindAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns<EndPoint, CancellationToken>((e, token) =>
                {
                    var mockTransport = new Mock<IConnectionListener>();
                    mockTransport
                        .Setup(transport => transport.AcceptAsync(It.IsAny<CancellationToken>()))
                        .Returns(new ValueTask<ConnectionContext>(result: null));
                    mockTransport
                        .Setup(transport => transport.EndPoint)
                        .Returns(e);

                    mockTransports.Add(mockTransport);

                    return new ValueTask<IConnectionListener>(mockTransport.Object);
                });

            // Don't use "using". Dispose() could hang if test fails.
            var server = new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { mockTransportFactory.Object }, mockLoggerFactory.Object);

            await server.StartAsync(new DummyApplication(), CancellationToken.None).DefaultTimeout();

            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5000), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5001), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5002), It.IsAny<CancellationToken>()), Times.Never);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5003), It.IsAny<CancellationToken>()), Times.Never);

            Assert.Equal(2, mockTransports.Count);

            foreach (var mockTransport in mockTransports)
            {
                mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Never);
            }

            currentConfig = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5002"),
                new KeyValuePair<string, string>("Endpoints:C:Url", "http://*:5003"),
            }).Build();

            await changeCallback().DefaultTimeout();

            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5000), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5001), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5002), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5003), It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(4, mockTransports.Count);

            foreach (var mockTransport in mockTransports)
            {
                if (((IPEndPoint)mockTransport.Object.EndPoint).Port == 5001)
                {
                    mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
                }
                else
                {
                    mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Never);
                }
            }

            currentConfig = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5002"),
                new KeyValuePair<string, string>("Endpoints:C:Url", "http://*:5003"),
                new KeyValuePair<string, string>("Endpoints:C:Protocols", "Http1"),
            }).Build();

            await changeCallback().DefaultTimeout();

            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5000), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5001), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5002), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5003), It.IsAny<CancellationToken>()), Times.Exactly(2));

            Assert.Equal(5, mockTransports.Count);

            var firstPort5003TransportChecked = false;

            foreach (var mockTransport in mockTransports)
            {
                var port = ((IPEndPoint)mockTransport.Object.EndPoint).Port;
                if (port == 5001)
                {
                    mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
                }
                else if (port == 5003 && !firstPort5003TransportChecked)
                {
                    mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
                    firstPort5003TransportChecked = true;
                }
                else
                {
                    mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Never);
                }
            }

            await server.StopAsync(CancellationToken.None).DefaultTimeout();

            foreach (var mockTransport in mockTransports)
            {
                mockTransport.Verify(t => t.UnbindAsync(It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Fact]
        public async Task DoesNotReloadOnConfigurationChangeByDefault()
        {
            var currentConfig = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5001"),
            }).Build();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns<string>(name => currentConfig.GetSection(name));
            mockConfig.Setup(c => c.GetChildren()).Returns(() => currentConfig.GetChildren());

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(mockLoggerFactory.Object);
            serviceCollection.AddSingleton(Mock.Of<ILogger<KestrelServer>>());

            var options = new KestrelServerOptions
            {
                ApplicationServices = serviceCollection.BuildServiceProvider(),
            };

            options.Configure(mockConfig.Object);

            var mockTransports = new List<Mock<IConnectionListener>>();
            var mockTransportFactory = new Mock<IConnectionListenerFactory>();
            mockTransportFactory
                .Setup(transportFactory => transportFactory.BindAsync(It.IsAny<EndPoint>(), It.IsAny<CancellationToken>()))
                .Returns<EndPoint, CancellationToken>((e, token) =>
                {
                    var mockTransport = new Mock<IConnectionListener>();
                    mockTransport
                        .Setup(transport => transport.AcceptAsync(It.IsAny<CancellationToken>()))
                        .Returns(new ValueTask<ConnectionContext>(result: null));
                    mockTransport
                        .Setup(transport => transport.EndPoint)
                        .Returns(e);

                    mockTransports.Add(mockTransport);

                    return new ValueTask<IConnectionListener>(mockTransport.Object);
                });

            // Don't use "using". Dispose() could hang if test fails.
            var server = new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { mockTransportFactory.Object }, mockLoggerFactory.Object);

            await server.StartAsync(new DummyApplication(), CancellationToken.None).DefaultTimeout();

            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5000), It.IsAny<CancellationToken>()), Times.Once);
            mockTransportFactory.Verify(f => f.BindAsync(new IPEndPoint(IPAddress.IPv6Any, 5001), It.IsAny<CancellationToken>()), Times.Once);

            mockConfig.Verify(c => c.GetReloadToken(), Times.Never);

            await server.StopAsync(CancellationToken.None).DefaultTimeout();
        }

        private static KestrelServer CreateServer(KestrelServerOptions options, ILogger testLogger)
        {
            return new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { new MockTransportFactory() }, new LoggerFactory(new[] { new KestrelTestLoggerProvider(testLogger) }));
        }

        private static KestrelServer CreateServer(KestrelServerOptions options, bool throwOnCriticalErrors = true)
        {
            return new KestrelServer(Options.Create(options), new List<IConnectionListenerFactory>() { new MockTransportFactory() }, new LoggerFactory(new[] { new KestrelTestLoggerProvider(throwOnCriticalErrors) }));
        }

        private static void StartDummyApplication(IServer server)
        {
            server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None).GetAwaiter().GetResult();
        }

        private class MockTransportFactory : IConnectionListenerFactory
        {
            public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
            {
                var mock = new Mock<IConnectionListener>();
                mock.Setup(m => m.EndPoint).Returns(endpoint);
                return new ValueTask<IConnectionListener>(mock.Object);
            }
        }

        private class ThrowingTransportFactory : IConnectionListenerFactory
        {
            public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
