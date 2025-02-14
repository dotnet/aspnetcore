// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

#nullable enable

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthCheckPublisherHostedServiceTest
{
    private static class DefaultHealthCheckEventIds
    {
        public static readonly EventId HealthCheckProcessingBegin = new EventId(DefaultHealthCheckService.EventIds.HealthCheckProcessingBeginId, DefaultHealthCheckService.EventIds.HealthCheckProcessingBeginName);
        public static readonly EventId HealthCheckProcessingEnd = new EventId(DefaultHealthCheckService.EventIds.HealthCheckProcessingEndId, DefaultHealthCheckService.EventIds.HealthCheckProcessingEndName);
        public static readonly EventId HealthCheckBegin = new EventId(DefaultHealthCheckService.EventIds.HealthCheckBeginId, DefaultHealthCheckService.EventIds.HealthCheckBeginName);
        public static readonly EventId HealthCheckEnd = new EventId(DefaultHealthCheckService.EventIds.HealthCheckEndId, DefaultHealthCheckService.EventIds.HealthCheckEndName);
    }
    private static class HealthCheckPublisherEventIds
    {
        public static readonly EventId HealthCheckPublisherProcessingBegin = new EventId(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingBeginId, HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingBeginName);
        public static readonly EventId HealthCheckPublisherProcessingEnd = new EventId(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingEndId, HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingEndName);
        public static readonly EventId HealthCheckPublisherBegin = new EventId(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherBeginId, HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherBeginName);
        public static readonly EventId HealthCheckPublisherEnd = new EventId(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherEndId, HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherEndName);
        public static readonly EventId HealthCheckPublisherError = new EventId(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherErrorId, HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherErrorName);
        public static readonly EventId HealthCheckPublisherTimeout = new EventId(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherTimeoutId, HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherTimeoutName);
    }

    [Fact]
    public async Task StartAsync_WithoutPublishers_DoesNotStartTimer()
    {
        // Arrange
        var publishers = new IHealthCheckPublisher[]
        {
        };

        var service = CreateService(publishers);

        try
        {
            // Act
            await service.StartAsync();

            // Assert
            Assert.False(service.IsTimerRunning);
            Assert.False(service.IsStopping);
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }
    }

    [Fact]
    public async Task StartAsync_WithPublishers_StartsTimer()
    {
        // Arrange
        var publishers = new IHealthCheckPublisher[]
        {
                new TestPublisher(),
        };

        var service = CreateService(publishers);

        try
        {
            // Act
            await service.StartAsync();

            // Assert
            Assert.True(service.IsTimerRunning);
            Assert.False(service.IsStopping);
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }
    }

    [Fact]
    public async Task StartAsync_WithPublishers_StartsTimer_RunsPublishers()
    {
        // Arrange
        var unblock0 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unblock1 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unblock2 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publishers = new TestPublisher[]
        {
                new TestPublisher() { Wait = unblock0.Task, },
                new TestPublisher() { Wait = unblock1.Task, },
                new TestPublisher() { Wait = unblock2.Task, },
        };

        var service = CreateService(publishers, configurePublisherOptions: (options) =>
        {
            options.Delay = TimeSpan.FromMilliseconds(0);
        });

        try
        {
            // Act
            await service.StartAsync();

            await publishers[0].Started.TimeoutAfter(TimeSpan.FromSeconds(10));
            await publishers[1].Started.TimeoutAfter(TimeSpan.FromSeconds(10));
            await publishers[2].Started.TimeoutAfter(TimeSpan.FromSeconds(10));

            unblock0.SetResult(null);
            unblock1.SetResult(null);
            unblock2.SetResult(null);

            // Assert
            Assert.True(service.IsTimerRunning);
            Assert.False(service.IsStopping);
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }
    }

    [Fact]
    public async Task StopAsync_CancelsExecution()
    {
        // Arrange
        var unblock = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publisher = new TestPublisher() { Wait = unblock.Task, };

        var service = CreateService(new[] { publisher });

        try
        {
            await service.StartAsync();

            // Start execution
            var running = RunServiceAsync(service);

            // Wait for the publisher to see the cancellation token
            await publisher.Started.TimeoutAfter(TimeSpan.FromSeconds(10));
            Assert.Single(publisher.Entries);

            // Act
            await service.StopAsync(); // Trigger cancellation

            // Assert
            await AssertCanceledAsync(publisher.Entries[0].cancellationToken);
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);

            unblock.SetResult(null);

            await running.TimeoutAfter(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }
    }

    [Fact]
    public async Task RunAsync_WaitsForCompletion_Single()
    {
        // Arrange
        var sink = new TestSink();

        var unblock = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publisher = new TestPublisher() { Wait = unblock.Task, };

        var service = CreateService(new[] { publisher }, sink: sink);

        try
        {
            await service.StartAsync();

            // Act
            var running = RunServiceAsync(service);

            await publisher.Started.TimeoutAfter(TimeSpan.FromSeconds(10));

            unblock.SetResult(null);

            await running.TimeoutAfter(TimeSpan.FromSeconds(10));

            // Assert
            Assert.True(service.IsTimerRunning);
            Assert.False(service.IsStopping);

            var report = Assert.Single(publisher.Entries).report;
            Assert.Equal(new[] { "one", "two", }, report.Entries.Keys.OrderBy(k => k));
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }

        Assert.Collection(
            sink.Writes,
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherProcessingBegin, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckProcessingBegin, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckBegin, entry.EventId); },
            entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckEventIds.HealthCheckBegin, DefaultHealthCheckEventIds.HealthCheckEnd }); },
            entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckEventIds.HealthCheckBegin, DefaultHealthCheckEventIds.HealthCheckEnd }); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckEnd, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckProcessingEnd, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherBegin, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherEnd, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherProcessingEnd, entry.EventId); });
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/49745")]
    public async Task RunAsync_WaitsForCompletion_Single_RegistrationParameters()
    {
        // Arrange
        const string HealthyMessage = "Everything is A-OK";

        var unblock = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unblockDelayedCheck = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publisher = new TestPublisher() { Wait = unblock.Task, };

        var service = CreateService(new[] { publisher }, configureBuilder: b =>
        {
            b.Add(
                new HealthCheckRegistration(
                    name: "CheckDefault",
                    instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                    failureStatus: null,
                    tags: null));

            b.Add(
                new HealthCheckRegistration(
                    name: "CheckDelay1Period9",
                    instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                    failureStatus: null,
                    tags: null,
                    timeout: default)
                {
                    Delay = TimeSpan.FromSeconds(1),
                    Period = TimeSpan.FromSeconds(9)
                });

            b.Add(
               new HealthCheckRegistration(
                   name: "CheckDelay1Period9_1",
                   instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                   failureStatus: null,
                   tags: null,
                   timeout: default)
               {
                   Delay = TimeSpan.FromSeconds(1),
                   Period = TimeSpan.FromSeconds(9)
               });

            b.Add(
               new HealthCheckRegistration(
                   name: "CheckDelay1Period18",
                   instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                   failureStatus: null,
                   tags: null,
                   timeout: default)
               {
                   Delay = TimeSpan.FromSeconds(1),
                   Period = TimeSpan.FromSeconds(18)
               });

            b.Add(
                new HealthCheckRegistration(
                    name: "CheckDelay2Period18",
                    instance: new DelegateHealthCheck(_ =>
                    {
                        unblockDelayedCheck.TrySetResult(null); // Unblock 2s delayed check
                        return Task.FromResult(HealthCheckResult.Healthy(HealthyMessage));
                    }),
                    failureStatus: null,
                    tags: null,
                    timeout: default)
                {
                    Delay = TimeSpan.FromSeconds(2),
                    Period = TimeSpan.FromSeconds(18)
                });

            b.Add(
                new HealthCheckRegistration(
                    name: "CheckDelay7Period11",
                    instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                    failureStatus: null,
                    tags: null,
                    timeout: default)
                {
                    Delay = TimeSpan.FromSeconds(7),
                    Period = TimeSpan.FromSeconds(11)
                });

            b.Add(
               new HealthCheckRegistration(
                   name: "CheckDelay9Period5",
                   instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                   failureStatus: null,
                   tags: null,
                   timeout: default)
               {
                   Delay = TimeSpan.FromSeconds(9),
                   Period = TimeSpan.FromSeconds(5)
               });

            b.Add(
               new HealthCheckRegistration(
                   name: "CheckDelay10Period8",
                   instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                   failureStatus: null,
                   tags: null,
                   timeout: default)
               {
                   Delay = TimeSpan.FromSeconds(10),
                   Period = TimeSpan.FromSeconds(8)
               });

            b.Add(
               new HealthCheckRegistration(
                   name: "CheckDelay10Period9",
                   instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                   failureStatus: null,
                   tags: null,
                   timeout: default)
               {
                   Delay = TimeSpan.FromSeconds(10),
                   Period = TimeSpan.FromSeconds(9)
               });
        });

        try
        {
            var running = RunServiceAsync(service);

            await publisher.Started.TimeoutAfter(TimeSpan.FromSeconds(10));

            await Task.Yield();
            Assert.False(running.IsCompleted);

            unblock.SetResult(null);

            await running.TimeoutAfter(TimeSpan.FromSeconds(10));

            // The timer hasn't started yet. Only the default 5 minute registration is run by RunServiceAsync.
            Assert.Equal("CheckDefault", Assert.Single(Assert.Single(publisher.Entries).report.Entries.Keys));

            await service.StartAsync();
            await unblockDelayedCheck.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            Assert.True(service.IsTimerRunning);
            Assert.False(service.IsStopping);
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }

        // Assert - after stop
        var entries = publisher.Entries.SelectMany(e => e.report.Entries.Select(e2 => e2.Key)).OrderBy(k => k).ToArray();
        Assert.Contains("CheckDefault", entries);
        Assert.Contains("CheckDelay1Period18", entries);
        Assert.Contains("CheckDelay1Period9", entries);
        Assert.Contains("CheckDelay1Period9_1", entries);
    }

    // Not testing logs here to avoid differences in logging order
    [Fact]
    public async Task RunAsync_WaitsForCompletion_Multiple()
    {
        // Arrange
        var unblock0 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unblock1 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unblock2 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publishers = new TestPublisher[]
        {
                new TestPublisher() { Wait = unblock0.Task, },
                new TestPublisher() { Wait = unblock1.Task, },
                new TestPublisher() { Wait = unblock2.Task, },
        };

        var service = CreateService(publishers);

        try
        {
            await service.StartAsync();

            // Act
            var running = RunServiceAsync(service);

            await publishers[0].Started.TimeoutAfter(TimeSpan.FromSeconds(10));
            await publishers[1].Started.TimeoutAfter(TimeSpan.FromSeconds(10));
            await publishers[2].Started.TimeoutAfter(TimeSpan.FromSeconds(10));

            unblock0.SetResult(null);
            unblock1.SetResult(null);
            unblock2.SetResult(null);

            await running.TimeoutAfter(TimeSpan.FromSeconds(10));

            // Assert
            Assert.True(service.IsTimerRunning);
            Assert.False(service.IsStopping);

            for (var i = 0; i < publishers.Length; i++)
            {
                var report = Assert.Single(publishers[i].Entries).report;
                Assert.Equal(new[] { "one", "two", }, report.Entries.Keys.OrderBy(k => k));
            }
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }
    }

    [Fact]
    public async Task RunAsync_PublishersCanTimeout()
    {
        // Arrange
        var sink = new TestSink();
        var unblock = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publisher = new TestPublisher() { Wait = unblock.Task, };

        var service = CreateService(new[] { publisher }, sink: sink);

        try
        {
            await service.StartAsync();

            // Act
            var running = RunServiceAsync(service);

            await publisher.Started.TimeoutAfter(TimeSpan.FromSeconds(10));

            service.CancelToken();

            await AssertCanceledAsync(publisher.Entries[0].cancellationToken);

            unblock.SetResult(null);

            await running.TimeoutAfter(TimeSpan.FromSeconds(10));

            // Assert
            Assert.True(service.IsTimerRunning);
            Assert.False(service.IsStopping);
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }

        Assert.Collection(
            sink.Writes,
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherProcessingBegin, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckProcessingBegin, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckBegin, entry.EventId); },
            entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckEventIds.HealthCheckBegin, DefaultHealthCheckEventIds.HealthCheckEnd }); },
            entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckEventIds.HealthCheckBegin, DefaultHealthCheckEventIds.HealthCheckEnd }); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckEnd, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckProcessingEnd, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherBegin, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherTimeout, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherProcessingEnd, entry.EventId); });
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/56245")]
    public async Task RunAsync_CanFilterHealthChecks()
    {
        // Arrange
        const string HealthyMessage = "Everything is A-OK";

        var unblockDelayedCheck = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var publishers = new TestPublisher[]
        {
                new TestPublisher(),
                new TestPublisher(),
        };

        var service = CreateService(
            publishers,
            configurePublisherOptions: (options) =>
            {
                options.Predicate = (r) => r.Name.Contains("Delay") && !r.Name.Contains("_2");
                options.Delay = TimeSpan.Zero;
            },
            configureBuilder: b =>
            {
                b.Add(
                new HealthCheckRegistration(
                    name: "CheckDefault",
                    instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                    failureStatus: null,
                    tags: null));

                b.Add(
                    new HealthCheckRegistration(
                        name: "CheckDelay1Period9",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                    {
                        Delay = TimeSpan.FromSeconds(1),
                        Period = TimeSpan.FromSeconds(9)
                    });

                b.Add(
                   new HealthCheckRegistration(
                        name: "CheckDelay1Period9_1",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                   {
                        Delay = TimeSpan.FromSeconds(1),
                        Period = TimeSpan.FromSeconds(9)
                   });

                b.Add(
                   new HealthCheckRegistration(
                        name: "CheckDelay1Period9_2",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                   {
                       Delay = TimeSpan.FromSeconds(1),
                       Period = TimeSpan.FromSeconds(9)
                   });

                b.Add(
                    new HealthCheckRegistration(
                        name: "CheckDelay2Period18",
                        instance: new DelegateHealthCheck(_ =>
                        {
                            unblockDelayedCheck.TrySetResult(null); // Unblock 2s delayed check
                            return Task.FromResult(HealthCheckResult.Healthy(HealthyMessage));
                        }),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                    {
                        Delay = TimeSpan.FromSeconds(2),
                        Period = TimeSpan.FromSeconds(18)
                    });

                b.Add(
                    new HealthCheckRegistration(
                        name: "CheckDelay7Period11",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                    {
                        Delay = TimeSpan.FromSeconds(7),
                        Period = TimeSpan.FromSeconds(11)
                    });

                b.Add(
                   new HealthCheckRegistration(
                        name: "CheckDelay9Period5",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                   {
                        Delay = TimeSpan.FromSeconds(9),
                        Period = TimeSpan.FromSeconds(5)
                   });

                b.Add(
                   new HealthCheckRegistration(
                        name: "CheckDelay10Period8",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                   {
                        Delay = TimeSpan.FromSeconds(10),
                        Period = TimeSpan.FromSeconds(8)
                   });

                b.Add(
                   new HealthCheckRegistration(
                        name: "CheckDelay10Period9",
                        instance: new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage))),
                        failureStatus: null,
                        tags: null,
                        timeout: default)
                   {
                        Delay = TimeSpan.FromSeconds(10),
                        Period = TimeSpan.FromSeconds(9)
                   });
            });

        try
        {
            await service.StartAsync();

            // Act
            await unblockDelayedCheck.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            // Assert
            for (var i = 0; i < publishers.Length; i++)
            {
                var entries = publishers[i].Entries.SelectMany(e => e.report.Entries.Select(e2 => e2.Key)).OrderBy(k => k).ToArray();

                Assert.Contains("CheckDelay1Period9", entries);
                Assert.Contains("CheckDelay1Period9_1", entries);
            }
        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }

        // Assert - after stop
        for (var i = 0; i < publishers.Length; i++)
        {
            var entries = publishers[i].Entries.SelectMany(e => e.report.Entries.Select(e2 => e2.Key)).OrderBy(k => k).ToArray();

            Assert.Contains("CheckDelay1Period9", entries);
            Assert.Contains("CheckDelay1Period9_1", entries);
        }
    }

    [Fact]
    public async Task RunAsync_HandlesExceptions()
    {
        // Arrange
        var sink = new TestSink();
        var publishers = new TestPublisher[]
        {
                new TestPublisher() { Exception = new InvalidTimeZoneException(), },
        };

        var service = CreateService(publishers, sink: sink);

        try
        {
            await service.StartAsync();

            // Act
            await RunServiceAsync(service).TimeoutAfter(TimeSpan.FromSeconds(10));

        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }

        Assert.Collection(
            sink.Writes,
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherProcessingBegin, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckProcessingBegin, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckBegin, entry.EventId); },
            entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckEventIds.HealthCheckBegin, DefaultHealthCheckEventIds.HealthCheckEnd }); },
            entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckEventIds.HealthCheckBegin, DefaultHealthCheckEventIds.HealthCheckEnd }); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckEnd, entry.EventId); },
            entry => { Assert.Equal(DefaultHealthCheckEventIds.HealthCheckProcessingEnd, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherBegin, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherError, entry.EventId); },
            entry => { Assert.Equal(HealthCheckPublisherEventIds.HealthCheckPublisherProcessingEnd, entry.EventId); });
    }

    // Not testing logging here to avoid flaky ordering issues
    [Fact]
    public async Task RunAsync_HandlesExceptions_Multiple()
    {
        // Arrange
        var sink = new TestSink();
        var publishers = new TestPublisher[]
        {
                new TestPublisher() { Exception = new InvalidTimeZoneException(), },
                new TestPublisher(),
                new TestPublisher() { Exception = new InvalidTimeZoneException(), },
        };

        var service = CreateService(publishers, sink: sink);

        try
        {
            await service.StartAsync();

            // Act
            await RunServiceAsync(service).TimeoutAfter(TimeSpan.FromSeconds(10));

        }
        finally
        {
            await service.StopAsync();
            Assert.False(service.IsTimerRunning);
            Assert.True(service.IsStopping);
        }
    }

    private HealthCheckPublisherHostedService CreateService(
        IHealthCheckPublisher[] publishers,
        Action<HealthCheckPublisherOptions>? configurePublisherOptions = null,
        Action<IHealthChecksBuilder>? configureBuilder = null,
        TestSink? sink = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddOptions();
        serviceCollection.AddLogging();

        IHealthChecksBuilder builder = serviceCollection.AddHealthChecks();
        if (configureBuilder == null)
        {
            builder.AddCheck("one", () => { return HealthCheckResult.Healthy(); })
                   .AddCheck("two", () => { return HealthCheckResult.Healthy(); });
        }
        else
        {
            configureBuilder(builder);
        }

        // Choosing big values for tests to make sure that we're not dependent on the defaults.
        // All of the tests that rely on the timer will set their own values for speed.
        serviceCollection.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromMinutes(5);
            options.Period = TimeSpan.FromMinutes(5);
            options.Timeout = TimeSpan.FromMinutes(5);
       });

        if (publishers != null)
        {
            for (var i = 0; i < publishers.Length; i++)
            {
                serviceCollection.AddSingleton<IHealthCheckPublisher>(publishers[i]);
            }
        }

        if (configurePublisherOptions != null)
        {
            serviceCollection.Configure(configurePublisherOptions);
        }

        if (sink != null)
        {
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
        }

        var services = serviceCollection.BuildServiceProvider();
        return services.GetServices<IHostedService>().OfType<HealthCheckPublisherHostedService>().Single();
    }

    private Task RunServiceAsync(HealthCheckPublisherHostedService service) => service.RunAsync((TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)));

    private static async Task AssertCanceledAsync(CancellationToken cancellationToken)
    {
        await Assert.ThrowsAsync<TaskCanceledException>(() => Task.Delay(TimeSpan.FromSeconds(10), cancellationToken));
    }

    private class TestPublisher : IHealthCheckPublisher
    {
        private TaskCompletionSource<object?> _started;

        public TestPublisher()
        {
            _started = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public List<(HealthReport report, CancellationToken cancellationToken)> Entries { get; } = new List<(HealthReport report, CancellationToken cancellationToken)>();

        public Exception? Exception { get; set; }

        public Task Started => _started.Task;

        public Task? Wait { get; set; }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            Entries.Add((report, cancellationToken));

            // Signal that we've started
            _started.SetResult(null);

            if (Wait != null)
            {
                await Wait;
            }

            if (Exception != null)
            {
                throw Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
