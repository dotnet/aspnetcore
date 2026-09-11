// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class KestrelServerTests
{
    private KestrelServerOptions CreateServerOptions()
    {
        // It's not actually going to be used - we just need to satisfy the check in ApplyDefaultCertificate
        var httpsConfig = new TestHttpsConfigurationService { IsInitialized = true };

        var serverOptions = new KestrelServerOptions();
        serverOptions.ApplicationServices = new ServiceCollection()
            .AddSingleton(new KestrelMetrics(new TestMeterFactory()))
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment())
            .AddSingleton<IHttpsConfigurationService>(httpsConfig)
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
            Assert.Equal(0, testLogger.CriticalErrorsLogged);
        }
    }

    [Fact]
    public void StartWithHttpsAddressConfiguresHttpsEndpoints()
    {
        var options = CreateServerOptions();
        options.TestOverrideDefaultCertificate = TestResources.GetTestCertificate();
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
        options.IsDevelopmentCertificateLoaded = true; // Prevent the system default from being loaded
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
            Assert.Equal(0, testLogger.CriticalErrorsLogged);
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
    [InlineData(null)] // Uses the default
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http2)]
    public void NoTlsLogging_None(HttpProtocols? protocols)
    {
        var (warnings, infos) = GetNoTlsLogging(protocols);
        Assert.Empty(warnings);
        Assert.Empty(infos);
    }

    [Theory]
    [InlineData(HttpProtocols.Http1 | HttpProtocols.Http3)]
    public void NoTlsLogging_Info(HttpProtocols? protocols)
    {
        var (warnings, infos) = GetNoTlsLogging(protocols);
        Assert.Empty(warnings);
        Assert.Equal(2, infos.Count()); // ipv4 and ipv6
    }

    [Theory]
    [InlineData(HttpProtocols.Http1AndHttp2)]
    public void NoTlsLogging_Warn(HttpProtocols? protocols)
    {
        var (warnings, infos) = GetNoTlsLogging(protocols);
        Assert.Equal(2, warnings.Count()); // ipv4 and ipv6
        Assert.Empty(infos);
    }

    [Theory]
    [InlineData(HttpProtocols.Http1AndHttp2AndHttp3)]
    public void NoTlsLogging_WarnAndInfo(HttpProtocols? protocols)
    {
        var (warnings, infos) = GetNoTlsLogging(protocols);
        Assert.Equal(2, warnings.Count()); // ipv4 and ipv6
        Assert.Equal(2, infos.Count()); // ipv4 and ipv6
    }

    private static (IEnumerable<TestApplicationErrorLogger.LogMessage> warnings, IEnumerable<TestApplicationErrorLogger.LogMessage> infos) GetNoTlsLogging(HttpProtocols? protocols)
    {
        var testLogger = new TestApplicationErrorLogger();
        var kestrelOptions = new KestrelServerOptions();

        if (protocols.HasValue)
        {
            kestrelOptions.ConfigureEndpointDefaults(opt =>
            {
                opt.Protocols = protocols.Value;
            });
        }

        using (var server = CreateServer(kestrelOptions, testLogger))
        {
            StartDummyApplication(server);
            return (testLogger.Messages.Where(log => log.EventId == 64), testLogger.Messages.Where(log => log.EventId == 65));
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
            Assert.Equal(0, testLogger.CriticalErrorsLogged);
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
            Assert.Equal(0, testLogger.CriticalErrorsLogged);
        }
    }

    [Fact]
    public void LoggerCategoryNameIsKestrelServerNamespace()
    {
        var loggerFactory = new TestLoggerFactory();
        new KestrelServer(Options.Create<KestrelServerOptions>(null), new MockTransportFactory(), loggerFactory);
        Assert.Contains("Microsoft.AspNetCore.Server.Kestrel", loggerFactory.Categories);
    }

    [Fact]
    public void ConstructorWithNullTransportFactoryThrows()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new KestrelServer(
                Options.Create<KestrelServerOptions>(null),
                null,
                new LoggerFactory(new[] { new KestrelTestLoggerProvider() })));

        Assert.Equal("transportFactory", exception.ParamName);
    }

    private static KestrelServerImpl CreateKestrelServer(
        KestrelServerOptions options,
        IEnumerable<IConnectionListenerFactory> transportFactories,
        IEnumerable<IMultiplexedConnectionListenerFactory> multiplexedFactories,
        ILoggerFactory loggerFactory = null,
        KestrelMetrics metrics = null)
    {
        var httpsConfigurationService = new HttpsConfigurationService();
        if (options?.ApplicationServices is IServiceProvider serviceProvider)
        {
            httpsConfigurationService.Initialize(
                serviceProvider.GetRequiredService<IHostEnvironment>(),
                serviceProvider.GetRequiredService<ILogger<KestrelServer>>(),
                serviceProvider.GetRequiredService<ILogger<HttpsConnectionMiddleware>>());
        }
	
        return new KestrelServerImpl(
            Options.Create<KestrelServerOptions>(options),
            transportFactories,
            multiplexedFactories,
            httpsConfigurationService,
            loggerFactory ?? new LoggerFactory(new[] { new KestrelTestLoggerProvider() }),
            diagnosticSource: null,
            metrics ?? new KestrelMetrics(new TestMeterFactory()),
            heartbeatHandlers: []);
    }

    [Fact]
    public void ConstructorWithNoTransportFactoriesThrows()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateKestrelServer(
                options: null,
                new List<IConnectionListenerFactory>(),
                Array.Empty<IMultiplexedConnectionListenerFactory>()));

        Assert.Equal(CoreStrings.TransportNotFound, exception.Message);
    }

    [Fact]
    public void StartWithMultipleTransportFactoriesDoesNotThrow()
    {
        using var server = CreateKestrelServer(
            CreateServerOptions(),
            new List<IConnectionListenerFactory>() { new ThrowingTransportFactory(), new MockTransportFactory() },
            Array.Empty<IMultiplexedConnectionListenerFactory>());

        StartDummyApplication(server);
    }

    [Fact]
    public async Task StartWithNoValidTransportFactoryThrows()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.Listen(new IPEndPoint(IPAddress.Loopback, 0));

        using var server = CreateKestrelServer(
                serverOptions,
                new List<IConnectionListenerFactory> { new NonBindableTransportFactory() },
                Array.Empty<IMultiplexedConnectionListenerFactory>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None));

        Assert.Equal("No registered IConnectionListenerFactory supports endpoint IPEndPoint: 127.0.0.1:0", exception.Message);
    }

    [Fact]
    public async Task StartWithMultipleTransportFactories_UseSupported()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
        var serverOptions = CreateServerOptions();
        serverOptions.Listen(endpoint);

        var transportFactory = new MockTransportFactory();

        using var server = CreateKestrelServer(
                serverOptions,
                new List<IConnectionListenerFactory> { transportFactory, new NonBindableTransportFactory() },
                Array.Empty<IMultiplexedConnectionListenerFactory>());

        await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None);

        Assert.Collection(transportFactory.BoundEndPoints,
            ep => Assert.Equal(endpoint, ep.OriginalEndPoint));
    }

    [Fact]
    public async Task StartWithNoValidTransportFactoryThrows_Http3()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.Listen(new IPEndPoint(IPAddress.Loopback, 0), c =>
        {
            c.Protocols = HttpProtocols.Http3;
            c.UseHttps(TestResources.GetTestCertificate());
        });

        using var server = CreateKestrelServer(
                serverOptions,
                new List<IConnectionListenerFactory>(),
                new List<IMultiplexedConnectionListenerFactory> { new NonBindableMultiplexedTransportFactory() });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None));

        Assert.Equal("No registered IMultiplexedConnectionListenerFactory supports endpoint IPEndPoint: 127.0.0.1:0", exception.Message);
    }

    [Fact]
    public async Task StartWithMultipleTransportFactories_Http3_UseSupported()
    {
        var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
        var serverOptions = CreateServerOptions();
        serverOptions.Listen(endpoint, c =>
        {
            c.Protocols = HttpProtocols.Http3;
            c.UseHttps(TestResources.GetTestCertificate());
        });

        var transportFactory = new MockMultiplexedTransportFactory();

        using var server = CreateKestrelServer(
                serverOptions,
                new List<IConnectionListenerFactory>(),
                new List<IMultiplexedConnectionListenerFactory> { transportFactory, new NonBindableMultiplexedTransportFactory() });

        await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None);

        Assert.Collection(transportFactory.BoundEndPoints,
            ep => Assert.Equal(endpoint, ep.OriginalEndPoint));
    }

    [Fact]
    public async Task ListenWithCustomEndpoint_DoesNotThrow()
    {
        var options = CreateServerOptions();

        var customEndpoint = new UriEndPoint(new("http://localhost:5000"));
        options.Listen(customEndpoint, options =>
        {
            options.UseHttps(TestResources.GetTestCertificate());
            options.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        });

        var mockTransportFactory = new MockTransportFactory();
        var mockMultiplexedTransportFactory = new MockMultiplexedTransportFactory();

        using var server = CreateKestrelServer(
            options,
            new List<IConnectionListenerFactory>() { mockTransportFactory },
            new List<IMultiplexedConnectionListenerFactory>() { mockMultiplexedTransportFactory });

        await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None);

        var transportEndPoint = Assert.Single(mockTransportFactory.BoundEndPoints);
        var multiplexedTransportEndPoint = Assert.Single(mockMultiplexedTransportFactory.BoundEndPoints);

        Assert.Same(customEndpoint, transportEndPoint.BoundEndPoint);
        Assert.Same(customEndpoint, multiplexedTransportEndPoint.BoundEndPoint);
    }

    [Fact]
    public async Task ListenIPWithStaticPort_TransportsGetIPv6Any()
    {
        var options = CreateServerOptions();
        options.ListenAnyIP(5000, options =>
        {
            options.UseHttps(TestResources.GetTestCertificate());
            options.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        });

        var mockTransportFactory = new MockTransportFactory();
        var mockMultiplexedTransportFactory = new MockMultiplexedTransportFactory();

        using var server = CreateKestrelServer(
            options,
            new List<IConnectionListenerFactory>() { mockTransportFactory },
            new List<IMultiplexedConnectionListenerFactory>() { mockMultiplexedTransportFactory });

        await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None);

        var transportEndPoint = Assert.Single(mockTransportFactory.BoundEndPoints);
        var multiplexedTransportEndPoint = Assert.Single(mockMultiplexedTransportFactory.BoundEndPoints);

        // Both transports should get the IPv6Any
        Assert.Equal(IPAddress.IPv6Any, ((IPEndPoint)transportEndPoint.OriginalEndPoint).Address);
        Assert.Equal(IPAddress.IPv6Any, ((IPEndPoint)multiplexedTransportEndPoint.OriginalEndPoint).Address);

        Assert.Equal(5000, ((IPEndPoint)transportEndPoint.OriginalEndPoint).Port);
        Assert.Equal(5000, ((IPEndPoint)multiplexedTransportEndPoint.OriginalEndPoint).Port);
    }

    [Fact]
    public async Task ListenIPWithEphemeralPort_TransportsGetIPv6Any()
    {
        var options = CreateServerOptions();
        options.ListenAnyIP(0, options =>
        {
            options.UseHttps(TestResources.GetTestCertificate());
            options.Protocols = HttpProtocols.Http1AndHttp2;
        });

        var mockTransportFactory = new MockTransportFactory();
        var mockMultiplexedTransportFactory = new MockMultiplexedTransportFactory();

        using var server = CreateKestrelServer(
            options,
            new List<IConnectionListenerFactory>() { mockTransportFactory },
            new List<IMultiplexedConnectionListenerFactory>() { mockMultiplexedTransportFactory });

        await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None);

        var transportEndPoint = Assert.Single(mockTransportFactory.BoundEndPoints);

        Assert.Equal(IPAddress.IPv6Any, ((IPEndPoint)transportEndPoint.OriginalEndPoint).Address);

        // Should have been assigned a random value.
        Assert.NotEqual(0, ((IPEndPoint)transportEndPoint.BoundEndPoint).Port);
    }

    [Fact]
    public async Task ListenIPWithEphemeralPort_MultiplexedTransportsGetIPv6Any()
    {
        var options = CreateServerOptions();
        options.ListenAnyIP(0, options =>
        {
            options.UseHttps(TestResources.GetTestCertificate());
            options.Protocols = HttpProtocols.Http3;
        });

        var mockTransportFactory = new MockTransportFactory();
        var mockMultiplexedTransportFactory = new MockMultiplexedTransportFactory();

        using var server = CreateKestrelServer(
            options,
            new List<IConnectionListenerFactory>() { mockTransportFactory },
            new List<IMultiplexedConnectionListenerFactory>() { mockMultiplexedTransportFactory });

        await server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None);

        var multiplexedTransportEndPoint = Assert.Single(mockMultiplexedTransportFactory.BoundEndPoints);

        Assert.Equal(IPAddress.IPv6Any, ((IPEndPoint)multiplexedTransportEndPoint.OriginalEndPoint).Address);

        // Should have been assigned a random value.
        Assert.NotEqual(0, ((IPEndPoint)multiplexedTransportEndPoint.BoundEndPoint).Port);
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

        var transport = new TestConnectionListener
        {
            AcceptAsyncFunc = _ => new ValueTask<ConnectionContext>((ConnectionContext)null),
            UnbindAsyncFunc = _ => new ValueTask(unbind.WaitAsync()),
            DisposeAsyncFunc = () => new ValueTask(stop.WaitAsync())
        };
        var transportFactory = new TestConnectionListenerFactory((endpoint, _) =>
        {
            transport.EndPoint = endpoint;
            return new ValueTask<IConnectionListener>(transport);
        });

        var server = new KestrelServer(Options.Create(options), transportFactory, new TestLoggerFactory());
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

        Assert.Equal(1, transport.UnbindAsyncCount);
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

        var transport = new TestConnectionListener
        {
            AcceptAsyncFunc = _ => new ValueTask<ConnectionContext>((ConnectionContext)null),
            UnbindAsyncFunc = async _ =>
            {
                await unbind.WaitAsync();
                throw unbindException;
            }
        };
        var transportFactory = new TestConnectionListenerFactory((endpoint, _) =>
        {
            transport.EndPoint = endpoint;
            return new ValueTask<IConnectionListener>(transport);
        });

        var server = new KestrelServer(Options.Create(options), transportFactory, new TestLoggerFactory());
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

        Assert.Equal(1, transport.UnbindAsyncCount);
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

        var transport = new TestConnectionListener
        {
            AcceptAsyncFunc = _ => new ValueTask<ConnectionContext>((ConnectionContext)null),
            UnbindAsyncFunc = _ => new ValueTask(unbindTcs.Task)
        };
        var transportFactory = new TestConnectionListenerFactory((endpoint, _) =>
        {
            transport.EndPoint = endpoint;
            return new ValueTask<IConnectionListener>(transport);
        });

        var server = new KestrelServer(Options.Create(options), transportFactory, new TestLoggerFactory());
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

        Assert.Equal(1, transport.UnbindAsyncCount);
    }

    [Fact]
    public void StartingServerInitializesHeartbeat()
    {
        var timeProvider = new FakeTimeProvider();
        var testContext = new TestServiceContext
        {
            ServerOptions =
                {
                    CodeBackedListenOptions =
                    {
                        new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                    }
                },
            FakeTimeProvider = timeProvider,
            TimeProvider = timeProvider,
            DateHeaderValueManager = new DateHeaderValueManager(timeProvider)
        };

        testContext.Heartbeat = new Heartbeat(
            new IHeartbeatHandler[] { testContext.DateHeaderValueManager },
            timeProvider,
            DebuggerWrapper.Singleton,
            testContext.Log,
            Heartbeat.Interval);

        using (var server = new KestrelServerImpl(new[] { new MockTransportFactory() }, Array.Empty<IMultiplexedConnectionListenerFactory>(), new HttpsConfigurationService(), testContext))
        {
            Assert.Null(testContext.DateHeaderValueManager.GetDateHeaderValues());

            // Ensure KestrelServer is started at a different time than when it was constructed, since we're
            // verifying the heartbeat is initialized during KestrelServer.StartAsync().
            testContext.FakeTimeProvider.Advance(TimeSpan.FromDays(1));

            StartDummyApplication(server);

            Assert.Equal(HeaderUtilities.FormatDate(testContext.FakeTimeProvider.GetUtcNow()),
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

        var changeToken = new TestChangeToken();
        changeToken.OnRegistered = () =>
        {
            changeCallbackRegisteredTcs?.SetResult();

            changeCallback = () =>
            {
                changeCallbackRegisteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                changeToken.Trigger();
                return changeCallbackRegisteredTcs.Task;
            };
        };

        var config = new TestConfiguration(currentConfig, changeToken);
        var loggerFactory = new TestLoggerFactory { Logger = NullLogger.Instance };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        serviceCollection.AddSingleton<ILogger<KestrelServer>>(NullLogger<KestrelServer>.Instance);
        serviceCollection.AddSingleton<ILogger<HttpsConnectionMiddleware>>(NullLogger<HttpsConnectionMiddleware>.Instance);
        serviceCollection.AddSingleton<ILogger<CertificatePathWatcher>>(NullLogger<CertificatePathWatcher>.Instance);
        serviceCollection.AddSingleton<IHttpsConfigurationService>(new TestHttpsConfigurationService());

        var options = new KestrelServerOptions
        {
            ApplicationServices = serviceCollection.BuildServiceProvider(),
        };

        options.Configure(config, reloadOnChange: true);

        var transports = new List<TestConnectionListener>();
        var transportFactory = new TestConnectionListenerFactory((e, token) =>
        {
            var transport = new TestConnectionListener
            {
                EndPoint = e,
                AcceptAsyncFunc = _ => new ValueTask<ConnectionContext>(result: null)
            };

            transports.Add(transport);

            return new ValueTask<IConnectionListener>(transport);
        });

        // Don't use "using". Dispose() could hang if test fails.
        var server = new KestrelServer(Options.Create(options), transportFactory, loggerFactory);

        await server.StartAsync(new DummyApplication(), CancellationToken.None).DefaultTimeout();

        Assert.Equal(1, transportFactory.CountBindsTo(5000));
        Assert.Equal(1, transportFactory.CountBindsTo(5001));
        Assert.Equal(0, transportFactory.CountBindsTo(5002));
        Assert.Equal(0, transportFactory.CountBindsTo(5003));

        Assert.Equal(2, transports.Count);

        foreach (var transport in transports)
        {
            Assert.Equal(0, transport.UnbindAsyncCount);
        }

        config.Current = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5002"),
                new KeyValuePair<string, string>("Endpoints:C:Url", "http://*:5003"),
            }).Build();

        await changeCallback().DefaultTimeout();

        Assert.Equal(1, transportFactory.CountBindsTo(5000));
        Assert.Equal(1, transportFactory.CountBindsTo(5001));
        Assert.Equal(1, transportFactory.CountBindsTo(5002));
        Assert.Equal(1, transportFactory.CountBindsTo(5003));

        Assert.Equal(4, transports.Count);

        foreach (var transport in transports)
        {
            if (((IPEndPoint)transport.EndPoint).Port == 5001)
            {
                Assert.Equal(1, transport.UnbindAsyncCount);
            }
            else
            {
                Assert.Equal(0, transport.UnbindAsyncCount);
            }
        }

        config.Current = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5002"),
                new KeyValuePair<string, string>("Endpoints:C:Url", "http://*:5003"),
                new KeyValuePair<string, string>("Endpoints:C:Protocols", "Http1"),
            }).Build();

        await changeCallback().DefaultTimeout();

        Assert.Equal(1, transportFactory.CountBindsTo(5000));
        Assert.Equal(1, transportFactory.CountBindsTo(5001));
        Assert.Equal(1, transportFactory.CountBindsTo(5002));
        Assert.Equal(2, transportFactory.CountBindsTo(5003));

        Assert.Equal(5, transports.Count);

        var firstPort5003TransportChecked = false;

        foreach (var transport in transports)
        {
            var port = ((IPEndPoint)transport.EndPoint).Port;
            if (port == 5001)
            {
                Assert.Equal(1, transport.UnbindAsyncCount);
            }
            else if (port == 5003 && !firstPort5003TransportChecked)
            {
                Assert.Equal(1, transport.UnbindAsyncCount);
                firstPort5003TransportChecked = true;
            }
            else
            {
                Assert.Equal(0, transport.UnbindAsyncCount);
            }
        }

        await server.StopAsync(CancellationToken.None).DefaultTimeout();

        foreach (var transport in transports)
        {
            Assert.Equal(1, transport.UnbindAsyncCount);
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

        var config = new TestConfiguration(currentConfig);
        var loggerFactory = new TestLoggerFactory { Logger = NullLogger.Instance };

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        serviceCollection.AddSingleton<ILogger<KestrelServer>>(NullLogger<KestrelServer>.Instance);
        serviceCollection.AddSingleton<ILogger<HttpsConnectionMiddleware>>(NullLogger<HttpsConnectionMiddleware>.Instance);
        serviceCollection.AddSingleton<IHttpsConfigurationService>(new TestHttpsConfigurationService());

        var options = new KestrelServerOptions
        {
            ApplicationServices = serviceCollection.BuildServiceProvider(),
        };

        options.Configure(config);

        var transports = new List<TestConnectionListener>();
        var transportFactory = new TestConnectionListenerFactory((e, token) =>
        {
            var transport = new TestConnectionListener
            {
                EndPoint = e,
                AcceptAsyncFunc = _ => new ValueTask<ConnectionContext>(result: null)
            };

            transports.Add(transport);

            return new ValueTask<IConnectionListener>(transport);
        });

        // Don't use "using". Dispose() could hang if test fails.
        var server = new KestrelServer(Options.Create(options), transportFactory, loggerFactory);

        await server.StartAsync(new DummyApplication(), CancellationToken.None).DefaultTimeout();

        Assert.Equal(1, transportFactory.CountBindsTo(5000));
        Assert.Equal(1, transportFactory.CountBindsTo(5001));

        Assert.Equal(0, config.GetReloadTokenCount);

        await server.StopAsync(CancellationToken.None).DefaultTimeout();
    }

    private static KestrelServer CreateServer(KestrelServerOptions options, ILogger testLogger)
    {
        return new KestrelServer(Options.Create(options), new MockTransportFactory(), new LoggerFactory(new[] { new KestrelTestLoggerProvider(testLogger) }));
    }

    private static KestrelServer CreateServer(KestrelServerOptions options, bool throwOnCriticalErrors = true)
    {
        return new KestrelServer(Options.Create(options), new MockTransportFactory(), new LoggerFactory(new[] { new KestrelTestLoggerProvider(throwOnCriticalErrors) }));
    }

    private static void StartDummyApplication(IServer server)
    {
        server.StartAsync(new DummyApplication(context => Task.CompletedTask), CancellationToken.None).GetAwaiter().GetResult();
    }

    private class MockTransportFactory : IConnectionListenerFactory
    {
        public List<BindDetail> BoundEndPoints { get; } = new List<BindDetail>();

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            EndPoint resolvedEndPoint = endpoint;
            if (resolvedEndPoint is IPEndPoint ipEndPoint)
            {
                var port = ipEndPoint.Port == 0
                    ? Random.Shared.Next(IPEndPoint.MinPort, IPEndPoint.MaxPort)
                    : ipEndPoint.Port;

                resolvedEndPoint = new IPEndPoint(new IPAddress(ipEndPoint.Address.GetAddressBytes()), port);
            }

            BoundEndPoints.Add(new BindDetail(endpoint, resolvedEndPoint));

            return new ValueTask<IConnectionListener>(new TestConnectionListener { EndPoint = resolvedEndPoint });
        }
    }

    private class ThrowingTransportFactory : IConnectionListenerFactory
    {
        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }
    }

    private class NonBindableTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
    {
        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }

        public bool CanBind(EndPoint endpoint) => false;
    }

    private class NonBindableMultiplexedTransportFactory : IMultiplexedConnectionListenerFactory, IConnectionListenerFactorySelector
    {
        public ValueTask<IMultiplexedConnectionListener> BindAsync(EndPoint endpoint, IFeatureCollection features = null, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }

        public bool CanBind(EndPoint endpoint) => false;
    }

    private class MockMultiplexedTransportFactory : IMultiplexedConnectionListenerFactory
    {
        public List<BindDetail> BoundEndPoints { get; } = new List<BindDetail>();

        public ValueTask<IMultiplexedConnectionListener> BindAsync(EndPoint endpoint, IFeatureCollection features = null, CancellationToken cancellationToken = default)
        {
            EndPoint resolvedEndPoint = endpoint;
            if (resolvedEndPoint is IPEndPoint ipEndPoint)
            {
                var port = ipEndPoint.Port == 0
                    ? Random.Shared.Next(IPEndPoint.MinPort, IPEndPoint.MaxPort)
                    : ipEndPoint.Port;

                resolvedEndPoint = new IPEndPoint(new IPAddress(ipEndPoint.Address.GetAddressBytes()), port);
            }

            BoundEndPoints.Add(new BindDetail(endpoint, resolvedEndPoint));

            return new ValueTask<IMultiplexedConnectionListener>(new TestMultiplexedConnectionListener { EndPoint = resolvedEndPoint });
        }
    }

    private sealed class TestConnectionListenerFactory : IConnectionListenerFactory
    {
        private readonly Func<EndPoint, CancellationToken, ValueTask<IConnectionListener>> _bindAsync;

        public TestConnectionListenerFactory(Func<EndPoint, CancellationToken, ValueTask<IConnectionListener>> bindAsync)
        {
            _bindAsync = bindAsync;
        }

        public List<EndPoint> BoundEndPoints { get; } = new List<EndPoint>();

        public int CountBindsTo(int port) => BoundEndPoints.Count(endpoint => endpoint is IPEndPoint ipEndPoint && ipEndPoint.Port == port);

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            BoundEndPoints.Add(endpoint);
            return _bindAsync(endpoint, cancellationToken);
        }
    }

    private sealed class TestConnectionListener : IConnectionListener
    {
        public EndPoint EndPoint { get; set; }

        public Func<CancellationToken, ValueTask<ConnectionContext>> AcceptAsyncFunc { get; set; }
            = _ => new ValueTask<ConnectionContext>((ConnectionContext)null);

        public Func<CancellationToken, ValueTask> UnbindAsyncFunc { get; set; } = _ => default;

        public Func<ValueTask> DisposeAsyncFunc { get; set; } = () => default;

        public int UnbindAsyncCount { get; private set; }

        public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default) => AcceptAsyncFunc(cancellationToken);

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            UnbindAsyncCount++;
            return UnbindAsyncFunc(cancellationToken);
        }

        public ValueTask DisposeAsync() => DisposeAsyncFunc();
    }

    private sealed class TestMultiplexedConnectionListener : IMultiplexedConnectionListener
    {
        public EndPoint EndPoint { get; set; }

        public ValueTask<MultiplexedConnectionContext> AcceptAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
            => new ValueTask<MultiplexedConnectionContext>((MultiplexedConnectionContext)null);

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default) => default;

        public ValueTask DisposeAsync() => default;
    }

    private sealed class TestChangeToken : IChangeToken
    {
        private Action<object> _callback;
        private object _state;

        public bool HasChanged { get; set; }

        public bool ActiveChangeCallbacks { get; set; } = true;

        public Action OnRegistered { get; set; } = () => { };

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            _callback = callback;
            _state = state;
            OnRegistered();
            return new TestDisposable();
        }

        public void Trigger()
        {
            _callback(_state);
        }
    }

    private sealed class TestConfiguration : IConfiguration
    {
        public TestConfiguration(IConfiguration current, IChangeToken reloadToken = null)
        {
            Current = current;
            ReloadToken = reloadToken ?? new TestChangeToken();
        }

        public IConfiguration Current { get; set; }

        public IChangeToken ReloadToken { get; set; }

        public int GetReloadTokenCount { get; private set; }

        public string this[string key]
        {
            get => Current[key];
            set => Current[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Current.GetChildren();

        public IChangeToken GetReloadToken()
        {
            GetReloadTokenCount++;
            return ReloadToken;
        }

        public IConfigurationSection GetSection(string key) => Current.GetSection(key);
    }

    private record BindDetail(EndPoint OriginalEndPoint, EndPoint BoundEndPoint);
}
