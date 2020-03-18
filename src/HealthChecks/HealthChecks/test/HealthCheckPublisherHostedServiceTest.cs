// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class HealthCheckPublisherHostedServiceTest
    {
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
            var publishers = new  IHealthCheckPublisher[]
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
            var unblock0 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var unblock1 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var unblock2 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var publishers = new TestPublisher[]
            {
                new TestPublisher() { Wait = unblock0.Task, },
                new TestPublisher() { Wait = unblock1.Task, },
                new TestPublisher() { Wait = unblock2.Task, },
            };

            var service = CreateService(publishers, configure: (options) =>
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
            var unblock = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var publishers = new TestPublisher[]
            {
                new TestPublisher() { Wait = unblock.Task, }
            };

            var service = CreateService(publishers);

            try
            {
                await service.StartAsync();

                // Start execution
                var running = service.RunAsync();

                // Wait for the publisher to see the cancellation token
                await publishers[0].Started.TimeoutAfter(TimeSpan.FromSeconds(10));
                Assert.Single(publishers[0].Entries);

                // Act
                await service.StopAsync(); // Trigger cancellation

                // Assert
                await AssertCancelledAsync(publishers[0].Entries[0].cancellationToken);
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

            var unblock = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var publishers = new TestPublisher[]
            {
                new TestPublisher() { Wait = unblock.Task, },
            };

            var service = CreateService(publishers, sink: sink);

            try
            {
                await service.StartAsync();

                // Act
                var running = service.RunAsync();

                await publishers[0].Started.TimeoutAfter(TimeSpan.FromSeconds(10));

                unblock.SetResult(null);

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

            Assert.Collection(
                sink.Writes,
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingBegin, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckProcessingBegin, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckBegin, entry.EventId); },
                entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckService.EventIds.HealthCheckBegin, DefaultHealthCheckService.EventIds.HealthCheckEnd }); },
                entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckService.EventIds.HealthCheckBegin, DefaultHealthCheckService.EventIds.HealthCheckEnd }); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckEnd, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckProcessingEnd, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherBegin, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherEnd, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingEnd, entry.EventId); });
        }

        // Not testing logs here to avoid differences in logging order
        [Fact]
        public async Task RunAsync_WaitsForCompletion_Multiple()
        {
            // Arrange
            var unblock0 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var unblock1 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var unblock2 = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                var running = service.RunAsync();

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
        [QuarantinedTest]
        public async Task RunAsync_PublishersCanTimeout()
        {
            // Arrange
            var sink = new TestSink();
            var unblock = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var publishers = new TestPublisher[]
            {
                new TestPublisher() { Wait = unblock.Task, },
            };

            var service = CreateService(publishers, sink: sink, configure: (options) =>
            {
                options.Timeout = TimeSpan.FromMilliseconds(50);
            });

            try
            {
                await service.StartAsync();

                // Act
                var running = service.RunAsync();

                await publishers[0].Started.TimeoutAfter(TimeSpan.FromSeconds(10));

                await AssertCancelledAsync(publishers[0].Entries[0].cancellationToken);

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
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingBegin, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckProcessingBegin, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckBegin, entry.EventId); },
                entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckService.EventIds.HealthCheckBegin, DefaultHealthCheckService.EventIds.HealthCheckEnd }); },
                entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckService.EventIds.HealthCheckBegin, DefaultHealthCheckService.EventIds.HealthCheckEnd }); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckEnd, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckProcessingEnd, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherBegin, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherTimeout, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingEnd, entry.EventId); });
        }

        [Fact]
        public async Task RunAsync_CanFilterHealthChecks()
        {
            // Arrange
            var publishers = new TestPublisher[]
            {
                new TestPublisher(),
                new TestPublisher(),
            };

            var service = CreateService(publishers, configure: (options) =>
            {
                options.Predicate = (r) => r.Name == "one";
            });

            try
            {
                await service.StartAsync();

                // Act
                await service.RunAsync().TimeoutAfter(TimeSpan.FromSeconds(10));

                // Assert
                for (var i = 0; i < publishers.Length; i++)
                {
                    var report = Assert.Single(publishers[i].Entries).report;
                    Assert.Equal(new[] { "one", }, report.Entries.Keys.OrderBy(k => k));
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
                await service.RunAsync().TimeoutAfter(TimeSpan.FromSeconds(10));

            }
            finally
            {
                await service.StopAsync();
                Assert.False(service.IsTimerRunning);
                Assert.True(service.IsStopping);
            }

            Assert.Collection(
                sink.Writes,
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingBegin, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckProcessingBegin, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckBegin, entry.EventId); },
                entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckService.EventIds.HealthCheckBegin, DefaultHealthCheckService.EventIds.HealthCheckEnd }); },
                entry => { Assert.Contains(entry.EventId, new[] { DefaultHealthCheckService.EventIds.HealthCheckBegin, DefaultHealthCheckService.EventIds.HealthCheckEnd }); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckEnd, entry.EventId); },
                entry => { Assert.Equal(DefaultHealthCheckService.EventIds.HealthCheckProcessingEnd, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherBegin, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherError, entry.EventId); },
                entry => { Assert.Equal(HealthCheckPublisherHostedService.EventIds.HealthCheckPublisherProcessingEnd, entry.EventId); });
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
                await service.RunAsync().TimeoutAfter(TimeSpan.FromSeconds(10));

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
            Action<HealthCheckPublisherOptions> configure = null,
            TestSink sink = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.AddHealthChecks()
                .AddCheck("one", () => { return HealthCheckResult.Healthy(); })
                .AddCheck("two", () => { return HealthCheckResult.Healthy(); });

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

            if (configure != null)
            {
                serviceCollection.Configure(configure);
            }

            if (sink != null)
            {
                serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
            }

            var services = serviceCollection.BuildServiceProvider();
            return services.GetServices<IHostedService>().OfType< HealthCheckPublisherHostedService>().Single();
        }

        private static async Task AssertCancelledAsync(CancellationToken cancellationToken)
        {
            await Assert.ThrowsAsync<TaskCanceledException>(() => Task.Delay(TimeSpan.FromSeconds(10), cancellationToken));
        }

        private class TestPublisher : IHealthCheckPublisher
        {
            private TaskCompletionSource<object> _started;

            public TestPublisher()
            {
                _started = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public List<(HealthReport report, CancellationToken cancellationToken)> Entries { get; } = new List<(HealthReport report, CancellationToken cancellationToken)>();

            public Exception Exception { get; set; }

            public Task Started => _started.Task;

            public Task Wait { get; set; }

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
}
