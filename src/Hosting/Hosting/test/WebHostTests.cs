// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    public class WebHostTests
    {
        [Fact]
        public async Task WebHostThrowsWithNoServer()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateBuilder().Build().StartAsync());
            Assert.Equal("No service for type 'Microsoft.AspNetCore.Hosting.Server.IServer' has been registered.", ex.Message);
        }

        [Fact]
        public void UseStartupThrowsWithNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateBuilder().UseStartup((string)null));
        }

        [Fact]
        public async Task NoDefaultAddressesAndDoNotPreferHostingUrlsIfNotConfigured()
        {
            using (var host = CreateBuilder().UseFakeServer().Build())
            {
                await host.StartAsync();
                var serverAddressesFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.False(serverAddressesFeature.Addresses.Any());
                Assert.False(serverAddressesFeature.PreferHostingUrls);
            }
        }

        [Fact]
        public async Task UsesLegacyConfigurationForAddressesAndDoNotPreferHostingUrlsIfNotConfigured()
        {
            var data = new Dictionary<string, string>
            {
                { "server.urls", "http://localhost:5002" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            using (var host = CreateBuilder(config).UseFakeServer().Build())
            {
                await host.StartAsync();
                var serverAddressFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.Equal("http://localhost:5002", serverAddressFeature.Addresses.First());
                Assert.False(serverAddressFeature.PreferHostingUrls);
            }
        }

        [Fact]
        public void UsesConfigurationForAddressesAndDoNotPreferHostingUrlsIfNotConfigured()
        {
            var data = new Dictionary<string, string>
            {
                { "urls", "http://localhost:5003" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            using (var host = CreateBuilder(config).UseFakeServer().Build())
            {
                host.Start();
                var serverAddressFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.Equal("http://localhost:5003", serverAddressFeature.Addresses.First());
                Assert.False(serverAddressFeature.PreferHostingUrls);
            }
        }

        [Fact]
        public async Task UsesNewConfigurationOverLegacyConfigForAddressesAndDoNotPreferHostingUrlsIfNotConfigured()
        {
            var data = new Dictionary<string, string>
            {
                { "server.urls", "http://localhost:5003" },
                { "urls", "http://localhost:5009" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            using (var host = CreateBuilder(config).UseFakeServer().Build())
            {
                await host.StartAsync();
                var serverAddressFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.Equal("http://localhost:5009", serverAddressFeature.Addresses.First());
                Assert.False(serverAddressFeature.PreferHostingUrls);
            }
        }

        [Fact]
        public void DoNotPreferHostingUrlsWhenNoAddressConfigured()
        {
            using (var host = CreateBuilder().UseFakeServer().PreferHostingUrls(true).Build())
            {
                host.Start();
                var serverAddressesFeature = host.ServerFeatures.Get<IServerAddressesFeature>();
                Assert.Empty(serverAddressesFeature.Addresses);
                Assert.False(serverAddressesFeature.PreferHostingUrls);
            }
        }

        [Fact]
        public async Task PreferHostingUrlsWhenAddressIsConfigured()
        {
            var data = new Dictionary<string, string>
            {
                { "urls", "http://localhost:5003" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            using (var host = CreateBuilder(config).UseFakeServer().PreferHostingUrls(true).Build())
            {
                await host.StartAsync();
                Assert.True(host.ServerFeatures.Get<IServerAddressesFeature>().PreferHostingUrls);
            }
        }

        [Fact]
        public void WebHostCanBeStarted()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Start())
            {
                var server = (FakeServer)host.Services.GetRequiredService<IServer>();
                Assert.NotNull(host);
                Assert.Equal(1, server.StartInstances.Count);
                Assert.Equal(0, server.StartInstances[0].DisposeCalls);

                host.Dispose();

                Assert.Equal(1, server.StartInstances[0].DisposeCalls);
            }
        }

        [Fact]
        public async Task WebHostShutsDownWhenTokenTriggers()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
#pragma warning disable CS0618 // Type or member is obsolete
                var lifetime2 = host.Services.GetRequiredService<Extensions.Hosting.IApplicationLifetime>();
#pragma warning restore CS0618 // Type or member is obsolete
                var server = (FakeServer)host.Services.GetRequiredService<IServer>();

                var cts = new CancellationTokenSource();

                var runInBackground = host.RunAsync(cts.Token);

                // Wait on the host to be started
                lifetime.ApplicationStarted.WaitHandle.WaitOne();
                Assert.True(lifetime2.ApplicationStarted.IsCancellationRequested);

                Assert.Equal(1, server.StartInstances.Count);
                Assert.Equal(0, server.StartInstances[0].DisposeCalls);

                cts.Cancel();

                // Wait on the host to shutdown
                lifetime.ApplicationStopped.WaitHandle.WaitOne();
                Assert.True(lifetime2.ApplicationStopped.IsCancellationRequested);

                // Wait for RunAsync to finish to guarantee Disposal of WebHost
                await runInBackground;

                Assert.Equal(1, server.StartInstances[0].DisposeCalls);
            }
        }

        [Fact]
        public async Task WebHostStopAsyncUsesDefaultTimeoutIfGivenTokenDoesNotFire()
        {
            var data = new Dictionary<string, string>
            {
                { WebHostDefaults.ShutdownTimeoutKey, "1" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var server = new Mock<IServer>();
            server.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder(config)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(server.Object);
                })
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                await host.StartAsync();

                var cts = new CancellationTokenSource();

                // Purposefully don't trigger cts
                var task = host.StopAsync(cts.Token);

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))));
            }
        }

        [Fact]
        public async Task WebHostStopAsyncUsesDefaultTimeoutIfNoTokenProvided()
        {
            var data = new Dictionary<string, string>
            {
                { WebHostDefaults.ShutdownTimeoutKey, "1" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var server = new Mock<IServer>();
            server.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder(config)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(server.Object);
                })
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                await host.StartAsync();

                var task = host.StopAsync();

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))));
            }
        }

        [Fact]
        public async Task WebHostStopAsyncCanBeCancelledEarly()
        {
            var data = new Dictionary<string, string>
            {
                { WebHostDefaults.ShutdownTimeoutKey, "10" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            var server = new Mock<IServer>();
            server.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder(config)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(server.Object);
                })
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                await host.StartAsync();

                var cts = new CancellationTokenSource();

                var task = host.StopAsync(cts.Token);
                cts.Cancel();

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(8))));
            }
        }

        [Fact]
        public void WebHostApplicationLifetimeEventsOrderedCorrectlyDuringShutdown()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
                var applicationStartedEvent = new ManualResetEventSlim(false);
                var applicationStoppingEvent = new ManualResetEventSlim(false);
                var applicationStoppedEvent = new ManualResetEventSlim(false);
                var applicationStartedCompletedBeforeApplicationStopping = false;
                var applicationStoppingCompletedBeforeApplicationStopped = false;
                var applicationStoppedCompletedBeforeRunCompleted = false;

                lifetime.ApplicationStarted.Register(() =>
                {
                    applicationStartedEvent.Set();
                });

                lifetime.ApplicationStopping.Register(() =>
                {
                    // Check whether the applicationStartedEvent has been set
                    applicationStartedCompletedBeforeApplicationStopping = applicationStartedEvent.IsSet;

                    // Simulate work.
                    Thread.Sleep(1000);

                    applicationStoppingEvent.Set();
                });

                lifetime.ApplicationStopped.Register(() =>
                {
                    // Check whether the applicationStoppingEvent has been set
                    applicationStoppingCompletedBeforeApplicationStopped = applicationStoppingEvent.IsSet;
                    applicationStoppedEvent.Set();
                });

                var runHostAndVerifyApplicationStopped = Task.Run(async () =>
                {
                    await host.RunAsync();
                    // Check whether the applicationStoppingEvent has been set
                    applicationStoppedCompletedBeforeRunCompleted = applicationStoppedEvent.IsSet;
                });

                // Wait until application has started to shut down the host
                Assert.True(applicationStartedEvent.Wait(5000));

                // Trigger host shutdown on a separate thread
                Task.Run(() => lifetime.StopApplication());

                // Wait for all events and host.Run() to complete
                Assert.True(runHostAndVerifyApplicationStopped.Wait(5000));

                // Verify Ordering
                Assert.True(applicationStartedCompletedBeforeApplicationStopping);
                Assert.True(applicationStoppingCompletedBeforeApplicationStopped);
                Assert.True(applicationStoppedCompletedBeforeRunCompleted);
            }
        }

        [Fact]
        public async Task WebHostDisposesServiceProvider()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(s =>
                {
                    s.AddTransient<IFakeService, FakeService>();
                    s.AddSingleton<IFakeSingletonService, FakeService>();
                })
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                await host.StartAsync();

                var singleton = (FakeService)host.Services.GetService<IFakeSingletonService>();
                var transient = (FakeService)host.Services.GetService<IFakeService>();

                Assert.False(singleton.Disposed);
                Assert.False(transient.Disposed);

                await host.StopAsync();

                Assert.False(singleton.Disposed);
                Assert.False(transient.Disposed);

                host.Dispose();

                Assert.True(singleton.Disposed);
                Assert.True(transient.Disposed);
            }
        }

        [Fact]
        public async Task WebHostNotifiesApplicationStarted()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();
#pragma warning disable CS0618 // Type or member is obsolete
                var applicationLifetime2 = host.Services.GetService<Extensions.Hosting.IApplicationLifetime>();
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.False(applicationLifetime.ApplicationStarted.IsCancellationRequested);
                Assert.False(applicationLifetime2.ApplicationStarted.IsCancellationRequested);

                await host.StartAsync();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
                Assert.True(applicationLifetime2.ApplicationStarted.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task WebHostNotifiesAllIApplicationLifetimeCallbacksEvenIfTheyThrow()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();
#pragma warning disable CS0618 // Type or member is obsolete
                var applicationLifetime2 = host.Services.GetService<Extensions.Hosting.IApplicationLifetime>();
#pragma warning restore CS0618 // Type or member is obsolete

                var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
                var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);
                var stopped = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopped);

                var started2 = RegisterCallbacksThatThrow(applicationLifetime2.ApplicationStarted);
                var stopping2 = RegisterCallbacksThatThrow(applicationLifetime2.ApplicationStopping);
                var stopped2 = RegisterCallbacksThatThrow(applicationLifetime2.ApplicationStopped);

                await host.StartAsync();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
                Assert.True(applicationLifetime2.ApplicationStarted.IsCancellationRequested);
                Assert.True(started.All(s => s));
                Assert.True(started2.All(s => s));
                host.Dispose();
                Assert.True(stopping.All(s => s));
                Assert.True(stopping2.All(s => s));
                Assert.True(stopped.All(s => s));
                Assert.True(stopped2.All(s => s));
            }
        }

        [Fact]
        public async Task WebHostNotifiesAllIApplicationLifetimeEventsCallbacksEvenIfTheyThrow()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(services =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build())
            {
                await host.StartAsync();
                Assert.True(events1[0]);
                Assert.True(events2[0]);
                host.Dispose();
                Assert.True(events1[1]);
                Assert.True(events2[1]);
            }
        }

        [Fact]
        public async Task WebHostStopApplicationDoesNotFireStopOnHostedService()
        {
            var stoppingCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(services =>
                {
                    Action started = () =>
                    {
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));
                })
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
                lifetime.StopApplication();

                await host.StartAsync();

                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);
            }
            Assert.Equal(1, stoppingCalls);
            Assert.Equal(1, disposingCalls);
        }

        [Fact]
        public async Task HostedServiceCanInjectApplicationLifetime()
        {
            using (var host = CreateBuilder()
                   .UseFakeServer()
                   .ConfigureServices(services =>
                   {
                       services.AddSingleton<IHostedService, TestHostedService>();
                   })
                   .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
                lifetime.StopApplication();

                await host.StartAsync();
                var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.True(svc.StartCalled);

                await host.StopAsync();
                Assert.True(svc.StopCalled);
                host.Dispose();
            }
        }

        [Fact]
        public async Task HostedServiceStartNotCalledIfWebHostNotStarted()
        {
            using (var host = CreateBuilder()
                   .UseFakeServer()
                   .ConfigureServices(services =>
                   {
                       services.AddHostedService<TestHostedService>();
                   })
                   .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
                lifetime.StopApplication();

                var svc = (TestHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.False(svc.StartCalled);
                await host.StopAsync();
                Assert.False(svc.StopCalled);
                host.Dispose();
                Assert.False(svc.StopCalled);
                Assert.True(svc.DisposeCalled);
            }
        }

        [Fact]
        public async Task WebHostStopApplicationFiresStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(services =>
                {
                    Action started = () =>
                    {
                        startedCalls++;
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));
                })
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();

                Assert.Equal(0, startedCalls);

                await host.StartAsync();
                Assert.Equal(1, startedCalls);
                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);

                await host.StopAsync();

                Assert.Equal(1, startedCalls);
                Assert.Equal(1, stoppingCalls);
                Assert.Equal(0, disposingCalls);

                host.Dispose();

                Assert.Equal(1, startedCalls);
                Assert.Equal(1, stoppingCalls);
                Assert.Equal(1, disposingCalls);
            }
        }

        [Fact]
        public async Task WebHostDisposeApplicationFiresStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(services =>
                {
                    Action started = () =>
                    {
                        startedCalls++;
                    };

                    Action stopping = () =>
                    {
                        stoppingCalls++;
                    };

                    Action disposing = () =>
                    {
                        disposingCalls++;
                    };

                    services.AddSingleton<IHostedService>(_ => new DelegateHostedService(started, stopping, disposing));
                })
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();

                Assert.Equal(0, startedCalls);
                await host.StartAsync();
                Assert.Equal(1, startedCalls);
                Assert.Equal(0, stoppingCalls);
                Assert.Equal(0, disposingCalls);
                host.Dispose();

                Assert.Equal(1, stoppingCalls);
                Assert.Equal(1, disposingCalls);
            }
        }

        [Fact]
        public async Task WebHostNotifiesAllIHostedServicesAndIApplicationLifetimeCallbacksEvenIfTheyThrow()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(services =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();
#pragma warning disable CS0618 // Type or member is obsolete
                var applicationLifetime2 = host.Services.GetService<Extensions.Hosting.IApplicationLifetime>();
#pragma warning restore CS0618 // Type or member is obsolete

                var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
                var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);

                var started2 = RegisterCallbacksThatThrow(applicationLifetime2.ApplicationStarted);
                var stopping2 = RegisterCallbacksThatThrow(applicationLifetime2.ApplicationStopping);

                await host.StartAsync();
                Assert.True(events1[0]);
                Assert.True(events2[0]);
                Assert.True(started.All(s => s));
                Assert.True(started2.All(s => s));
                host.Dispose();
                Assert.True(events1[1]);
                Assert.True(events2[1]);
                Assert.True(stopping.All(s => s));
                Assert.True(stopping2.All(s => s));
            }
        }

        [Fact]
        public async Task WebHostInjectsHostingEnvironment()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .UseEnvironment("WithHostingEnvironment")
                .Build())
            {
                await host.StartAsync();
                var env = host.Services.GetService<IHostingEnvironment>();
#pragma warning disable CS0618 // Type or member is obsolete
                var env2 = host.Services.GetService<Extensions.Hosting.IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal("Changed", env.EnvironmentName);
                Assert.Equal("Changed", env2.EnvironmentName);
            }
        }

        [Fact]
        public void CanReplaceStartupLoader()
        {
            var builder = CreateBuilder()
                .ConfigureServices(services =>
                {
                    services.AddTransient<IStartup, TestStartup>();
                })
                .UseFakeServer()
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => builder.Build());
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            using (var host = CreateBuilder().UseFakeServer().ConfigureServices(services => services.AddOptions()).Build())
            {
                Assert.NotNull(host.Services.GetRequiredService<IOptions<object>>());
            }
        }

        [Fact]
        public void ConfiguresStartupFiltersInCorrectOrder()
        {
            // Verify ordering
            var configureOrder = 0;
            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices(services =>
                {
                    services.AddTransient<IStartupFilter>(serviceProvider => new TestFilter(
                        () => Assert.Equal(1, configureOrder++),
                        () => Assert.Equal(2, configureOrder++),
                        () => Assert.Equal(5, configureOrder++)));
                    services.AddTransient<IStartupFilter>(serviceProvider => new TestFilter(
                        () => Assert.Equal(0, configureOrder++),
                        () => Assert.Equal(3, configureOrder++),
                        () => Assert.Equal(4, configureOrder++)));
                })
                .Build())
            {
                host.Start();
                Assert.Equal(6, configureOrder);
            }
        }

        private class TestFilter : IStartupFilter
        {
            private readonly Action _verifyConfigureOrder;
            private readonly Action _verifyBuildBeforeOrder;
            private readonly Action _verifyBuildAfterOrder;

            public TestFilter(Action verifyConfigureOrder, Action verifyBuildBeforeOrder, Action verifyBuildAfterOrder)
            {
                _verifyConfigureOrder = verifyConfigureOrder;
                _verifyBuildBeforeOrder = verifyBuildBeforeOrder;
                _verifyBuildAfterOrder = verifyBuildAfterOrder;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                _verifyConfigureOrder();
                return builder =>
                {
                    _verifyBuildBeforeOrder();
                    next(builder);
                    _verifyBuildAfterOrder();
                };
            }
        }

        [Fact]
        public void EnvDefaultsToProductionIfNoConfig()
        {
            using (var host = CreateBuilder().UseFakeServer().Build())
            {
                var env = host.Services.GetService<IHostingEnvironment>();
#pragma warning disable CS0618 // Type or member is obsolete
                var env2 = host.Services.GetService<Extensions.Hosting.IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(EnvironmentName.Production, env.EnvironmentName);
                Assert.Equal(EnvironmentName.Production, env2.EnvironmentName);
            }
        }

        [Fact]
        public void EnvDefaultsToConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "Environment", EnvironmentName.Staging }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            using (var host = CreateBuilder(config).UseFakeServer().Build())
            {
                var env = host.Services.GetService<IHostingEnvironment>();
#pragma warning disable CS0618 // Type or member is obsolete
                var env2 = host.Services.GetService<Extensions.Hosting.IHostingEnvironment>();
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.Equal(EnvironmentName.Staging, env.EnvironmentName);
                Assert.Equal(EnvironmentName.Staging, env.EnvironmentName);
            }
        }

        [Fact]
        public void WebRootCanBeResolvedFromTheConfig()
        {
            var vals = new Dictionary<string, string>
            {
                { "webroot", "testroot" }
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            using (var host = CreateBuilder(config).UseFakeServer().Build())
            {
                var env = host.Services.GetService<IHostingEnvironment>();
                Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
                Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
            }
        }

        [Fact]
        public async Task IsEnvironment_Extension_Is_Case_Insensitive()
        {
            using (var host = CreateBuilder().UseFakeServer().Build())
            {
                await host.StartAsync();
                var env = host.Services.GetRequiredService<IHostingEnvironment>();
                Assert.True(env.IsEnvironment(EnvironmentName.Production));
                Assert.True(env.IsEnvironment("producTion"));
            }
        }

        [Fact]
        public async Task WebHost_CreatesDefaultRequestIdentifierFeature_IfNotPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
                {
                    httpContext = innerHttpContext;
                    return Task.FromResult(0);
                });

            using (var host = CreateHost(requestDelegate))
            {
                // Act
                await host.StartAsync();

                // Assert
                Assert.NotNull(httpContext);
                var featuresTraceIdentifier = httpContext.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
                Assert.False(string.IsNullOrWhiteSpace(httpContext.TraceIdentifier));
                Assert.Same(httpContext.TraceIdentifier, featuresTraceIdentifier);
            }
        }

        [Fact]
        public async Task WebHost_DoesNot_CreateDefaultRequestIdentifierFeature_IfPresent()
        {
            // Arrange
            HttpContext httpContext = null;
            var requestDelegate = new RequestDelegate(innerHttpContext =>
            {
                httpContext = innerHttpContext;
                return Task.FromResult(0);
            });
            var requestIdentifierFeature = new StubHttpRequestIdentifierFeature();

            using (var host = CreateHost(requestDelegate))
            {
                var server = (FakeServer)host.Services.GetRequiredService<IServer>();
                server.CreateRequestFeatures = () =>
                {
                    var features = FakeServer.NewFeatureCollection();
                    features.Set<IHttpRequestIdentifierFeature>(requestIdentifierFeature);
                    return features;
                };
                // Act
                await host.StartAsync();

                // Assert
                Assert.NotNull(httpContext);
                Assert.Same(requestIdentifierFeature, httpContext.Features.Get<IHttpRequestIdentifierFeature>());
            }
        }

        [Fact]
        public async Task WebHost_InvokesConfigureMethodsOnlyOnce()
        {
            using (var host = CreateBuilder()
                .UseFakeServer()
                .UseStartup<CountStartup>()
                .Build())
            {
                await host.StartAsync();
                var services = host.Services;
                var services2 = host.Services;
                Assert.Equal(1, CountStartup.ConfigureCount);
                Assert.Equal(1, CountStartup.ConfigureServicesCount);
            }
        }

        public class CountStartup
        {
            public static int ConfigureServicesCount;
            public static int ConfigureCount;

            public void ConfigureServices(IServiceCollection services)
            {
                ConfigureServicesCount++;
            }

            public void Configure(IApplicationBuilder app)
            {
                ConfigureCount++;
            }
        }

        [Fact]
        public void WebHost_ThrowsForBadConfigureServiceSignature()
        {
            var builder = CreateBuilder()
                .UseFakeServer()
                .UseStartup<BadConfigureServicesStartup>();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("ConfigureServices", ex.Message);
        }

        public class BadConfigureServicesStartup
        {
            public void ConfigureServices(IServiceCollection services, int gunk) { }
            public void Configure(IApplicationBuilder app) { }
        }

        private IWebHost CreateHost(RequestDelegate requestDelegate)
        {
            var builder = CreateBuilder()
                .UseFakeServer()
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddProvider(new AllMessagesAreNeeded());
                })
                .Configure(
                    appBuilder =>
                    {
                        appBuilder.Run(requestDelegate);
                    });
            return builder.Build();
        }

        private IWebHostBuilder CreateBuilder(IConfiguration config = null)
        {
            return new WebHostBuilder().UseConfiguration(config ?? new ConfigurationBuilder().Build()).UseStartup("Microsoft.AspNetCore.Hosting.Tests");
        }

        private static bool[] RegisterCallbacksThatThrow(IServiceCollection services)
        {
            bool[] events = new bool[2];

            Action started = () =>
            {
                events[0] = true;
                throw new InvalidOperationException();
            };

            Action stopping = () =>
            {
                events[1] = true;
                throw new InvalidOperationException();
            };

            services.AddSingleton<IHostedService>(new DelegateHostedService(started, stopping, () => { }));

            return events;
        }

        private static bool[] RegisterCallbacksThatThrow(CancellationToken token)
        {
            var signals = new bool[3];
            for (int i = 0; i < signals.Length; i++)
            {
                token.Register(state =>
                {
                    signals[(int)state] = true;
                    throw new InvalidOperationException();
                }, i);
            }

            return signals;
        }

        private class TestHostedService : IHostedService, IDisposable
        {
            private readonly IApplicationLifetime _lifetime;

#pragma warning disable CS0618 // Type or member is obsolete
            public TestHostedService(IApplicationLifetime lifetime, Extensions.Hosting.IApplicationLifetime lifetime2)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                _lifetime = lifetime;
            }

            public bool StartCalled { get; set; }
            public bool StopCalled { get; set; }
            public bool DisposeCalled { get; set; }

            public Task StartAsync(CancellationToken token)
            {
                StartCalled = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken token)
            {
                StopCalled = true;
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        private class DelegateHostedService : IHostedService, IDisposable
        {
            private readonly Action _started;
            private readonly Action _stopping;
            private readonly Action _disposing;

            public DelegateHostedService(Action started, Action stopping, Action disposing)
            {
                _started = started;
                _stopping = stopping;
                _disposing = disposing;
            }

            public Task StartAsync(CancellationToken token)
            {
                _started();
                return Task.CompletedTask;
            }
            public Task StopAsync(CancellationToken token)
            {
                _stopping();
                return Task.CompletedTask;
            }

            public void Dispose() => _disposing();
        }

        public class StartInstance : IDisposable
        {
            public int StopCalls { get; set; }

            public int DisposeCalls { get; set; }

            public void Stop()
            {
                StopCalls += 1;
            }

            public void Dispose()
            {
                DisposeCalls += 1;
            }
        }

        public class FakeServer : IServer
        {
            public FakeServer()
            {
                Features = new FeatureCollection();
                Features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
            }

            public IList<StartInstance> StartInstances { get; } = new List<StartInstance>();

            public Func<IFeatureCollection> CreateRequestFeatures { get; set; } = NewFeatureCollection;

            public IFeatureCollection Features { get; }

            public static IFeatureCollection NewFeatureCollection()
            {
                var stub = new StubFeatures();
                var features = new FeatureCollection();
                features.Set<IHttpRequestFeature>(stub);
                features.Set<IHttpResponseFeature>(stub);
                return features;
            }

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            {
                var startInstance = new StartInstance();
                StartInstances.Add(startInstance);
                var context = application.CreateContext(CreateRequestFeatures());
                try
                {
                    application.ProcessRequestAsync(context);
                }
                catch (Exception ex)
                {
                    application.DisposeContext(context, ex);
                    throw;
                }
                application.DisposeContext(context, null);

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                if (StartInstances != null)
                {
                    foreach (var startInstance in StartInstances)
                    {
                        startInstance.Stop();
                    }
                }

                return Task.CompletedTask;
            }

            public void Dispose()
            {
                if (StartInstances != null)
                {
                    foreach (var startInstance in StartInstances)
                    {
                        startInstance.Dispose();
                    }
                }
            }
        }

        private class TestStartup : IStartup
        {
            public void Configure(IApplicationBuilder app)
            {
                throw new NotImplementedException();
            }

            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                throw new NotImplementedException();
            }
        }

        private class ReadOnlyFeatureCollection : IFeatureCollection
        {
            public object this[Type key]
            {
                get { return null; }
                set { throw new NotSupportedException(); }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public int Revision
            {
                get { return 0; }
            }

            public void Dispose()
            {
            }

            public TFeature Get<TFeature>()
            {
                return default(TFeature);
            }

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
            {
                yield break;
            }

            public void Set<TFeature>(TFeature instance)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        private class AllMessagesAreNeeded : ILoggerProvider, ILogger
        {
            public bool IsEnabled(LogLevel logLevel) => true;

            public ILogger CreateLogger(string name) => this;

            public IDisposable BeginScope<TState>(TState state)
            {
                var stringified = state.ToString();
                return this;
            }
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var stringified = formatter(state, exception);
            }

            public void Dispose()
            {
            }
        }

        private class StubFeatures : IHttpRequestFeature, IHttpResponseFeature, IHeaderDictionary
        {
            public StubFeatures()
            {
                Headers = this;
                Body = new MemoryStream();
            }

            public StringValues this[string key]
            {
                get { return StringValues.Empty; }
                set { }
            }

            public Stream Body { get; set; }

            public long? ContentLength { get; set; }

            public int Count => 0;

            public bool HasStarted { get; set; }

            public IHeaderDictionary Headers { get; set; }

            public bool IsReadOnly => false;

            public ICollection<string> Keys => null;

            public string Method { get; set; }

            public string Path { get; set; }

            public string PathBase { get; set; }

            public string Protocol { get; set; }

            public string QueryString { get; set; }

            public string RawTarget { get; set; }

            public string ReasonPhrase { get; set; }

            public string Scheme { get; set; }

            public int StatusCode { get; set; }

            public ICollection<StringValues> Values => null;

            public void Add(KeyValuePair<string, StringValues> item) { }

            public void Add(string key, StringValues value) { }

            public void Clear() { }

            public bool Contains(KeyValuePair<string, StringValues> item) => false;

            public bool ContainsKey(string key) => false;

            public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) { }

            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => null;

            public void OnCompleted(Func<object, Task> callback, object state) { }

            public void OnStarting(Func<object, Task> callback, object state) { }

            public bool Remove(KeyValuePair<string, StringValues> item) => false;

            public bool Remove(string key) => false;

            public bool TryGetValue(string key, out StringValues value)
            {
                value = StringValues.Empty;
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator() => null;
        }

        private class StubHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
        {
            public string TraceIdentifier { get; set; }
        }
    }

    public static class TestServerWebHostExtensions
    {
        public static IWebHostBuilder UseFakeServer(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services => services.AddSingleton<IServer, WebHostTests.FakeServer>());
        }
    }
}