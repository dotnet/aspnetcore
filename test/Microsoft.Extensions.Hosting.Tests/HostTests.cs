// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Fakes;
using Microsoft.Extensions.Hosting.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class HostTests
    {
        [Fact]
        public async Task HostInjectsHostingEnvironment()
        {
            using (var host = CreateBuilder()
                .UseEnvironment("WithHostingEnvironment")
                .Build())
            {
                await host.StartAsync();
                var env = host.Services.GetService<IHostingEnvironment>();
                Assert.Equal("WithHostingEnvironment", env.EnvironmentName);
            }
        }

        [Fact]
        public void CanCreateApplicationServicesWithAddedServices()
        {
            using (var host = CreateBuilder().ConfigureServices((hostContext, services) => services.AddSingleton<IFakeService, FakeService>()).Build())
            {
                Assert.NotNull(host.Services.GetRequiredService<IFakeService>());
            }
        }

        [Fact]
        public void EnvDefaultsToProductionIfNoConfig()
        {
            using (var host = CreateBuilder().Build())
            {
                var env = host.Services.GetService<IHostingEnvironment>();
                Assert.Equal(EnvironmentName.Production, env.EnvironmentName);
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

            using (var host = CreateBuilder(config).Build())
            {
                var env = host.Services.GetService<IHostingEnvironment>();
                Assert.Equal(EnvironmentName.Staging, env.EnvironmentName);
            }
        }

        [Fact]
        public async Task IsEnvironment_Extension_Is_Case_Insensitive()
        {
            using (var host = CreateBuilder().Build())
            {
                await host.StartAsync();
                var env = host.Services.GetRequiredService<IHostingEnvironment>();
                Assert.True(env.IsEnvironment(EnvironmentName.Production));
                Assert.True(env.IsEnvironment("producTion"));
            }
        }

        [Fact]
        public void HostCanBeStarted()
        {
            FakeHostedService service;
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IHostedService, FakeHostedService>();
                })
                .Start())
            {
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.NotNull(host);
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void HostedServiceCanAcceptSingletonDependencies()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IFakeService, FakeService>();
                    services.AddHostedService<FakeHostedServiceWithDependency>();
                })
                .Start())
            {
            }
        }

        private class FakeHostedServiceWithDependency : IHostedService
        {
            public FakeHostedServiceWithDependency(IFakeService fakeService)
            {
                Assert.NotNull(fakeService);
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task HostedServiceStartNotCalledIfHostNotStarted()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((hostContext, services) =>
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
        public async Task HostCanBeStoppedWhenNotStarted()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((hostContext, services) =>
                   {
                       services.AddHostedService<TestHostedService>();
                   })
                   .Build())
            {
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
        public async Task AppCrashesOnStartWhenFirstHostedServiceThrows()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build())
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
                Assert.True(events1[0]);
                Assert.False(events2[0]);
                host.Dispose();
                // Stopping
                Assert.False(events1[1]);
                Assert.False(events2[1]);
            }
        }

        [Fact]
        public async Task StartCanBeCancelled()
        {
            var serviceStarting = new ManualResetEvent(false);
            var startCancelled = new ManualResetEvent(false);
            FakeHostedService service;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService>(_ => new FakeHostedService()
                    {
                        StartAction = ct =>
                        {
                            Assert.False(ct.IsCancellationRequested);
                            serviceStarting.Set();
                            Assert.True(startCancelled.WaitOne(TimeSpan.FromSeconds(5)));
                            ct.ThrowIfCancellationRequested();
                        }
                    });
                })
                .Build())
            {
                var cts = new CancellationTokenSource();

                var startTask = Task.Run(() => host.StartAsync(cts.Token));
                Assert.True(serviceStarting.WaitOne(TimeSpan.FromSeconds(5)));
                cts.Cancel();
                startCancelled.Set();
                await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);

                Assert.NotNull(host);
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task HostLifetimeOnStartedDelaysStart()
        {
            var serviceStarting = new ManualResetEvent(false);
            var lifetimeStart = new ManualResetEvent(false);
            var lifetimeContinue = new ManualResetEvent(false);
            FakeHostedService service;
            FakeHostLifetime lifetime;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService>(_ => new FakeHostedService()
                    {
                        StartAction = ct =>
                        {
                            serviceStarting.Set();
                        }
                    });
                    services.AddSingleton<IHostLifetime>(_ => new FakeHostLifetime()
                    {
                        StartAction = ct =>
                        {
                            lifetimeStart.Set();
                            Assert.True(lifetimeContinue.WaitOne(TimeSpan.FromSeconds(5)));
                        }
                    });
                })
                .Build())
            {
                var startTask = Task.Run(() => host.StartAsync());
                Assert.True(lifetimeStart.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.False(serviceStarting.WaitOne(0));

                lifetimeContinue.Set();
                Assert.True(serviceStarting.WaitOne(TimeSpan.FromSeconds(5)));

                await startTask;

                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                lifetime = (FakeHostLifetime)host.Services.GetRequiredService<IHostLifetime>();
                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(0, lifetime.StopCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);

            Assert.Equal(1, lifetime.StartCount);
            Assert.Equal(0, lifetime.StopCount);
        }

        [Fact]
        public async Task HostLifetimeOnStartedCanBeCancelled()
        {
            var serviceStarting = new ManualResetEvent(false);
            var lifetimeStart = new ManualResetEvent(false);
            var lifetimeContinue = new ManualResetEvent(false);
            FakeHostedService service;
            FakeHostLifetime lifetime;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService>(_ => new FakeHostedService()
                    {
                        StartAction = ct =>
                        {
                            serviceStarting.Set();
                        }
                    });
                    services.AddSingleton<IHostLifetime>(_ => new FakeHostLifetime()
                    {
                        StartAction = ct =>
                        {
                            lifetimeStart.Set();
                            WaitHandle.WaitAny(new[] { lifetimeContinue, ct.WaitHandle });
                        }
                    });
                })
                .Build())
            {
                var cts = new CancellationTokenSource();

                var startTask = Task.Run(() => host.StartAsync(cts.Token));

                Assert.True(lifetimeStart.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.False(serviceStarting.WaitOne(0));

                cts.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => startTask);
                Assert.False(serviceStarting.WaitOne(0));

                lifetimeContinue.Set();
                Assert.False(serviceStarting.WaitOne(0));

                Assert.NotNull(host);
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(0, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                lifetime = (FakeHostLifetime)host.Services.GetRequiredService<IHostLifetime>();
                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(0, lifetime.StopCount);
            }

            Assert.Equal(0, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);

            Assert.Equal(1, lifetime.StartCount);
            Assert.Equal(0, lifetime.StopCount);
        }

        [Fact]
        public async Task HostStopAsyncCallsHostLifetimeStopAsync()
        {
            FakeHostedService service;
            FakeHostLifetime lifetime;
            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<IHostedService, FakeHostedService>();
                    services.AddSingleton<IHostLifetime, FakeHostLifetime>();
                })
                .Build())
            {
                await host.StartAsync();

                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                lifetime = (FakeHostLifetime)host.Services.GetRequiredService<IHostLifetime>();
                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(0, lifetime.StopCount);

                await host.StopAsync();

                Assert.Equal(1, service.StartCount);
                Assert.Equal(1, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                Assert.Equal(1, lifetime.StartCount);
                Assert.Equal(1, lifetime.StopCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(1, service.StopCount);
            Assert.Equal(1, service.DisposeCount);

            Assert.Equal(1, lifetime.StartCount);
            Assert.Equal(1, lifetime.StopCount);
        }

        [Fact]
        public async Task HostShutsDownWhenTokenTriggers()
        {
            FakeHostedService service;
            using (var host = CreateBuilder()
                .ConfigureServices((services) => services.AddSingleton<IHostedService, FakeHostedService>())
                .Build())
            {
                var lifetime = host.Services.GetRequiredService<IApplicationLifetime>();
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();

                var cts = new CancellationTokenSource();

                var runInBackground = host.RunAsync(cts.Token);

                // Wait on the host to be started
                lifetime.ApplicationStarted.WaitHandle.WaitOne();

                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);

                cts.Cancel();

                // Wait on the host to shutdown
                lifetime.ApplicationStopped.WaitHandle.WaitOne();

                // Wait for RunAsync to finish to guarantee Disposal of Host
                await runInBackground;

                Assert.Equal(1, service.StopCount);
                Assert.Equal(1, service.DisposeCount);
            }
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task HostStopAsyncCanBeCancelledEarly()
        {
            var service = new Mock<IHostedService>();
            service.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton(service.Object);
                })
                .Build())
            {
                await host.StartAsync();

                var cts = new CancellationTokenSource();

                var task = host.StopAsync(cts.Token);
                cts.Cancel();

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))));
            }
        }

        [Fact]
        public async Task HostStopAsyncUsesDefaultTimeoutIfGivenTokenDoesNotFire()
        {
            var service = new Mock<IHostedService>();
            service.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(0.5));
                    services.AddSingleton(service.Object);
                })
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
            var service = new Mock<IHostedService>();
            service.Setup(s => s.StopAsync(It.IsAny<CancellationToken>()))
                .Returns<CancellationToken>(token =>
                {
                    return Task.Run(() =>
                    {
                        token.WaitHandle.WaitOne();
                    });
                });

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(0.5));
                    services.AddSingleton(service.Object);
                })
                .Build())
            {
                await host.StartAsync();

                var task = host.StopAsync();

                Assert.Equal(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))));
            }
        }

        [Fact]
        public void HostApplicationLifetimeEventsOrderedCorrectlyDuringShutdown()
        {
            using (var host = CreateBuilder()
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
        public async Task HostDisposesServiceProvider()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((s) =>
                {
                    s.AddTransient<IFakeService, FakeService>();
                    s.AddSingleton<IFakeSingletonService, FakeService>();
                })
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
        public async Task HostNotifiesApplicationStarted()
        {
            using (var host = CreateBuilder()
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

                Assert.False(applicationLifetime.ApplicationStarted.IsCancellationRequested);

                await host.StartAsync();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task HostNotifiesAllIApplicationLifetimeCallbacksEvenIfTheyThrow()
        {
            using (var host = CreateBuilder()
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

                var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
                var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);
                var stopped = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopped);

                await host.StartAsync();
                Assert.True(applicationLifetime.ApplicationStarted.IsCancellationRequested);
                Assert.True(started.All(s => s));
                await host.StopAsync();
                Assert.True(stopping.All(s => s));
                host.Dispose();
                Assert.True(stopped.All(s => s));
            }
        }

        [Fact]
        public async Task HostStopApplicationDoesNotFireStopOnHostedService()
        {
            var stoppingCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
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
            Assert.Equal(0, stoppingCalls);
            Assert.Equal(1, disposingCalls);
        }

        [Fact]
        public async Task HostedServiceCanInjectApplicationLifetime()
        {
            using (var host = CreateBuilder()
                   .ConfigureServices((services) =>
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
            }
        }

        [Fact]
        public async Task HostStopApplicationFiresStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
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
        public async Task HostDisposeApplicationDoesNotFireStopOnHostedService()
        {
            var stoppingCalls = 0;
            var startedCalls = 0;
            var disposingCalls = 0;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
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

                Assert.Equal(0, stoppingCalls);
                Assert.Equal(1, disposingCalls);
            }
        }

        [Fact]
        public async Task HostDoesNotNotifyIApplicationLifetimeCallbacksIfIHostedServicesThrow()
        {
            bool[] events1 = null;
            bool[] events2 = null;

            using (var host = CreateBuilder()
                .ConfigureServices((services) =>
                {
                    events1 = RegisterCallbacksThatThrow(services);
                    events2 = RegisterCallbacksThatThrow(services);
                })
                .Build())
            {
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

                var started = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStarted);
                var stopping = RegisterCallbacksThatThrow(applicationLifetime.ApplicationStopping);

                await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
                Assert.True(events1[0]);
                Assert.False(events2[0]);
                Assert.False(started.All(s => s));
                host.Dispose();
                Assert.False(events1[1]);
                Assert.False(events2[1]);
                Assert.False(stopping.All(s => s));
            }
        }

        [Fact]
        public async Task Host_InvokesConfigureServicesMethodsOnlyOnce()
        {
            int configureServicesCount = 0;
            using (var host = CreateBuilder()
                .ConfigureServices((services) => configureServicesCount++)
                .Build())
            {
                Assert.Equal(1, configureServicesCount);
                await host.StartAsync();
                var services = host.Services;
                var services2 = host.Services;
                Assert.Equal(1, configureServicesCount);
            }
        }

        private IHostBuilder CreateBuilder(IConfiguration config = null)
        {
            return new HostBuilder().ConfigureHostConfiguration(builder => builder.AddConfiguration(config ?? new ConfigurationBuilder().Build()));
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

            public TestHostedService(IApplicationLifetime lifetime)
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
    }
}