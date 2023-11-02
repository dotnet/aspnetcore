// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

#nullable enable

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public class DefaultHealthCheckServiceTest
{
    [Fact]
    public void Constructor_ThrowsUsefulExceptionForDuplicateNames()
    {
        // Arrange
        //
        // Doing this the old fashioned way so we can verify that the exception comes
        // from the constructor.
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddOptions();
        serviceCollection.AddHealthChecks()
            .AddCheck("Foo", new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy())))
            .AddCheck("Foo", new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy())))
            .AddCheck("Bar", new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy())))
            .AddCheck("Baz", new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy())))
            .AddCheck("Baz", new DelegateHealthCheck(_ => Task.FromResult(HealthCheckResult.Healthy())));

        var services = serviceCollection.BuildServiceProvider();

        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var options = services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        var logger = services.GetRequiredService<ILogger<DefaultHealthCheckService>>();

        // Act
        var exception = Assert.Throws<ArgumentException>(() => new DefaultHealthCheckService(scopeFactory, options, logger));

        // Assert
        Assert.StartsWith($"Duplicate health checks were registered with the name(s): Foo, Baz", exception.Message);
    }

    [Fact]
    public async Task CheckAsync_RunsAllChecksAndAggregatesResultsAsync()
    {
        const string DataKey = "Foo";
        const string DataValue = "Bar";
        const string DegradedMessage = "I'm not feeling so good";
        const string UnhealthyMessage = "Halp!";
        const string HealthyMessage = "Everything is A-OK";
        var exception = new Exception("Things are pretty bad!");
        var healthyCheckTags = new List<string> { "healthy-check-tag" };
        var degradedCheckTags = new List<string> { "degraded-check-tag" };
        var unhealthyCheckTags = new List<string> { "unhealthy-check-tag" };

        // Arrange
        var data = new Dictionary<string, object>()
            {
                { DataKey, DataValue }
            };

        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("HealthyCheck", _ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage, data)), healthyCheckTags);
            b.AddAsyncCheck("DegradedCheck", _ => Task.FromResult(HealthCheckResult.Degraded(DegradedMessage)), degradedCheckTags);
            b.AddAsyncCheck("UnhealthyCheck", _ => Task.FromResult(HealthCheckResult.Unhealthy(UnhealthyMessage, exception)), unhealthyCheckTags);
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries.OrderBy(kvp => kvp.Key),
            actual =>
            {
                Assert.Equal("DegradedCheck", actual.Key);
                Assert.Equal(DegradedMessage, actual.Value.Description);
                Assert.Equal(HealthStatus.Degraded, actual.Value.Status);
                Assert.Null(actual.Value.Exception);
                Assert.Empty(actual.Value.Data);
                Assert.Equal(actual.Value.Tags, degradedCheckTags);
            },
            actual =>
            {
                Assert.Equal("HealthyCheck", actual.Key);
                Assert.Equal(HealthyMessage, actual.Value.Description);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Null(actual.Value.Exception);
                Assert.Collection(actual.Value.Data, item =>
                {
                    Assert.Equal(DataKey, item.Key);
                    Assert.Equal(DataValue, item.Value);
                });
                Assert.Equal(actual.Value.Tags, healthyCheckTags);
            },
            actual =>
            {
                Assert.Equal("UnhealthyCheck", actual.Key);
                Assert.Equal(UnhealthyMessage, actual.Value.Description);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
                Assert.Same(exception, actual.Value.Exception);
                Assert.Empty(actual.Value.Data);
                Assert.Equal(actual.Value.Tags, unhealthyCheckTags);
            });
    }

    [Fact]
    public async Task CheckAsync_TagsArePresentInHealthReportEntryIfExceptionOccurs()
    {
        const string ExceptionMessage = "exception-message";
        const string OperationCancelledMessage = "operation-cancelled-message";
        var exceptionTags = new[] { "unhealthy-check-tag" };
        var operationExceptionTags = new[] { "degraded-check-tag" };

        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("ExceptionCheck", _ => throw new Exception(ExceptionMessage), exceptionTags);
            b.AddAsyncCheck("OperationExceptionCheck", _ => throw new OperationCanceledException(OperationCancelledMessage), operationExceptionTags);
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries.OrderBy(kvp => kvp.Key),
            actual =>
            {
                Assert.Equal("ExceptionCheck", actual.Key);
                Assert.Equal(ExceptionMessage, actual.Value.Description);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
                Assert.Equal(ExceptionMessage, actual.Value.Exception!.Message);
                Assert.Empty(actual.Value.Data);
                Assert.Equal(actual.Value.Tags, exceptionTags);
            },
            actual =>
            {
                Assert.Equal("OperationExceptionCheck", actual.Key);
                Assert.Equal("A timeout occurred while running check.", actual.Value.Description);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
                Assert.Equal(OperationCancelledMessage, actual.Value.Exception!.Message);
                Assert.Empty(actual.Value.Data);
                Assert.Equal(actual.Value.Tags, operationExceptionTags);
            });
    }

    [Fact]
    public async Task CheckAsync_RunsFilteredChecksAndAggregatesResultsAsync()
    {
        const string DataKey = "Foo";
        const string DataValue = "Bar";
        const string DegradedMessage = "I'm not feeling so good";
        const string UnhealthyMessage = "Halp!";
        const string HealthyMessage = "Everything is A-OK";
        var exception = new Exception("Things are pretty bad!");

        // Arrange
        var data = new Dictionary<string, object>
            {
                { DataKey, DataValue }
            };

        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("HealthyCheck", _ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage, data)));
            b.AddAsyncCheck("DegradedCheck", _ => Task.FromResult(HealthCheckResult.Degraded(DegradedMessage)));
            b.AddAsyncCheck("UnhealthyCheck", _ => Task.FromResult(HealthCheckResult.Unhealthy(UnhealthyMessage, exception)));
        });

        // Act
        var results = await service.CheckHealthAsync(c => c.Name == "HealthyCheck");

        // Assert
        Assert.Collection(results.Entries,
            actual =>
            {
                Assert.Equal("HealthyCheck", actual.Key);
                Assert.Equal(HealthyMessage, actual.Value.Description);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Null(actual.Value.Exception);
                Assert.Collection(actual.Value.Data, item =>
                {
                    Assert.Equal(DataKey, item.Key);
                    Assert.Equal(DataValue, item.Value);
                });
            });
    }

    [Fact]
    public async Task CheckHealthAsync_SetsRegistrationForEachCheck()
    {
        // Arrange
        var thrownException = new InvalidOperationException("Whoops!");
        var faultedException = new InvalidOperationException("Ohnoes!");

        var service = CreateHealthChecksService(b =>
        {
            b.AddCheck<NameCapturingCheck>("A");
            b.AddCheck<NameCapturingCheck>("B");
            b.AddCheck<NameCapturingCheck>("C");
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("A", actual.Key);
                Assert.Collection(
                    actual.Value.Data,
                    kvp => Assert.Equal(kvp, new KeyValuePair<string, object>("name", "A")));
            },
            actual =>
            {
                Assert.Equal("B", actual.Key);
                Assert.Collection(
                    actual.Value.Data,
                    kvp => Assert.Equal(kvp, new KeyValuePair<string, object>("name", "B")));
            },
            actual =>
            {
                Assert.Equal("C", actual.Key);
                Assert.Collection(
                    actual.Value.Data,
                    kvp => Assert.Equal(kvp, new KeyValuePair<string, object>("name", "C")));
            });
    }

    [Fact]
    public async Task CheckHealthAsync_Cancellation_CanPropagate()
    {
        // Arrange
        var insideCheck = new TaskCompletionSource<object?>();

        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("cancels", async ct =>
            {
                insideCheck.SetResult(null);

                await Task.Delay(10000, ct);
                return HealthCheckResult.Unhealthy();
            });
        });

        var cancel = new CancellationTokenSource();
        var task = service.CheckHealthAsync(cancel.Token);

        // After this returns we know the check has started
        await insideCheck.Task;

        cancel.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task CheckHealthAsync_ConvertsExceptionInHealthCheckToUnhealthyResultAsync()
    {
        // Arrange
        var thrownException = new InvalidOperationException("Whoops!");
        var faultedException = new InvalidOperationException("Ohnoes!");

        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("Throws", ct => throw thrownException);
            b.AddAsyncCheck("Faults", ct => Task.FromException<HealthCheckResult>(faultedException));
            b.AddAsyncCheck("Succeeds", ct => Task.FromResult(HealthCheckResult.Healthy()));
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("Throws", actual.Key);
                Assert.Equal(thrownException.Message, actual.Value.Description);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
                Assert.Same(thrownException, actual.Value.Exception);
            },
            actual =>
            {
                Assert.Equal("Faults", actual.Key);
                Assert.Equal(faultedException.Message, actual.Value.Description);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
                Assert.Same(faultedException, actual.Value.Exception);
            },
            actual =>
            {
                Assert.Equal("Succeeds", actual.Key);
                Assert.Null(actual.Value.Description);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
                Assert.Null(actual.Value.Exception);
            });
    }

    [Fact]
    public async Task CheckHealthAsync_SetsUpALoggerScopeForEachCheck()
    {
        // Arrange
        var sink = new TestSink();
        var check = new DelegateHealthCheck(cancellationToken =>
        {
            Assert.Collection(sink.Scopes,
                actual =>
                {
                    Assert.Equal(actual.LoggerName, typeof(DefaultHealthCheckService).FullName);
                    Assert.Collection((IEnumerable<KeyValuePair<string, object>>)actual.Scope,
                        item =>
                        {
                            Assert.Equal("HealthCheckName", item.Key);
                            Assert.Equal("TestScope", item.Value);
                        });
                });
            return Task.FromResult(HealthCheckResult.Healthy());
        });

        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var service = CreateHealthChecksService(b =>
        {
            // Override the logger factory for testing
            b.Services.AddSingleton<ILoggerFactory>(loggerFactory);

            b.AddCheck("TestScope", check);
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(results.Entries, actual =>
        {
            Assert.Equal("TestScope", actual.Key);
            Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
        });
    }

    [Fact]
    public async Task CheckHealthAsync_CheckCanDependOnTransientService()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.Services.AddTransient<AnotherService>();

            b.AddCheck<CheckWithServiceDependency>("Test");
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("Test", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
            });
    }

    [Fact]
    public async Task CheckHealthAsync_CheckCanDependOnScopedService()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.Services.AddScoped<AnotherService>();

            b.AddCheck<CheckWithServiceDependency>("Test");
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("Test", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
            });
    }

    [Fact]
    // related to issue https://github.com/dotnet/aspnetcore/issues/14453
    public async Task CheckHealthAsync_CheckCanDependOnScopedService_per_check()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.Services.AddScoped<CantBeMultiThreadedService>();

            b.AddCheck<CheckWithServiceNotMultiThreadDependency>("Test");
            b.AddCheck<CheckWithServiceNotMultiThreadDependency>("Test2");
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("Test", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
            },
            actual =>
            {
                Assert.Equal("Test2", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
            });
    }

    [Fact]
    public async Task CheckHealthAsync_CheckCanHaveScopedDisposableDependencies()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.Services.AddScoped<SynchronousDisposable>();
            b.Services.AddScoped<AsyncOnlyDisposable>();
            b.Services.AddScoped<SyncOrAsyncDisposable>();

            b.AddCheck<DisposableDependeciesCheck>("TestDisposableDepenencies");
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        var healthCheck = (DisposableDependeciesCheck)results.Entries.Single().Value.Data.Single().Value;

        Assert.True(healthCheck.SynchronousDisposable.IsDisposed);
        Assert.True(healthCheck.AsyncOnlyDisposable.IsAsyncDisposed);
        Assert.True(healthCheck.SyncOrAsyncDisposable.IsAsyncDisposed);
        Assert.False(healthCheck.SyncOrAsyncDisposable.IsDisposed);
    }

    [Fact]
    public async Task CheckHealthAsync_CheckCanDependOnSingletonService()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.Services.AddSingleton<AnotherService>();

            b.AddCheck<CheckWithServiceDependency>("Test");
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("Test", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
            });
    }

    [Fact]
    public async Task CheckHealthAsync_ChecksAreRunInParallel()
    {
        // Arrange
        var input1 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var input2 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output1 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output2 = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("test1",
                async () =>
                {
                    output1.SetResult(null);
                    await input1.Task;
                    return HealthCheckResult.Healthy();
                });
            b.AddAsyncCheck("test2",
                async () =>
                {
                    output2.SetResult(null);
                    await input2.Task;
                    return HealthCheckResult.Healthy();
                });
        });

        // Act
        var checkHealthTask = service.CheckHealthAsync();
        await Task.WhenAll(output1.Task, output2.Task).TimeoutAfter(TimeSpan.FromSeconds(10));
        input1.SetResult(null);
        input2.SetResult(null);
        await checkHealthTask;

        // Assert
        Assert.Collection(checkHealthTask.Result.Entries,
            entry =>
            {
                Assert.Equal("test1", entry.Key);
                Assert.Equal(HealthStatus.Healthy, entry.Value.Status);
            },
            entry =>
            {
                Assert.Equal("test2", entry.Key);
                Assert.Equal(HealthStatus.Healthy, entry.Value.Status);
            });
    }

    [Fact]
    public async Task CheckHealthAsync_TimeoutReturnsUnhealthy()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("timeout", async (ct) =>
            {
                await Task.Delay(2000, ct);
                return HealthCheckResult.Healthy();
            }, timeout: TimeSpan.FromMilliseconds(100));
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("timeout", actual.Key);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
            });
    }

    [Fact]
    public void CheckHealthAsync_WorksInSingleThreadedSyncContext()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.AddAsyncCheck("test", async () =>
            {
                await Task.Delay(1).ConfigureAwait(false);
                return HealthCheckResult.Healthy();
            });
        });

        var hangs = true;

        // Act
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
        {
            var token = cts.Token;
            token.Register(() => throw new OperationCanceledException(token));

            SingleThreadedSynchronizationContext.Run(() =>
            {
                // Act
                service.CheckHealthAsync(token).GetAwaiter().GetResult();
                hangs = false;
            });
        }

        // Assert
        Assert.False(hangs);
    }

    [Fact]
    public async Task CheckHealthAsync_WithFailureStatus()
    {
        // Arrange
        var service = CreateHealthChecksService(b =>
        {
            b.AddCheck<FailCapturingCheck>("degraded", HealthStatus.Degraded);
            b.AddCheck<FailCapturingCheck>("healthy", HealthStatus.Healthy);
            b.AddCheck<FailCapturingCheck>("unhealthy", HealthStatus.Unhealthy);
        });

        // Act
        var results = await service.CheckHealthAsync();

        // Assert
        Assert.Collection(
            results.Entries,
            actual =>
            {
                Assert.Equal("degraded", actual.Key);
                Assert.Equal(HealthStatus.Degraded, actual.Value.Status);
            },
            actual =>
            {
                Assert.Equal("healthy", actual.Key);
                Assert.Equal(HealthStatus.Healthy, actual.Value.Status);
            },
            actual =>
            {
                Assert.Equal("unhealthy", actual.Key);
                Assert.Equal(HealthStatus.Unhealthy, actual.Value.Status);
            });
    }

    private static DefaultHealthCheckService CreateHealthChecksService(Action<IHealthChecksBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        var builder = services.AddHealthChecks();
        if (configure != null)
        {
            configure(builder);
        }

        return (DefaultHealthCheckService)services.BuildServiceProvider(validateScopes: true).GetRequiredService<HealthCheckService>();
    }

    private class AnotherService { }

    private class CantBeMultiThreadedService
    {
        private readonly object _lock = new();
        private bool _wasUsed;
        public void Check()
        {
            lock (_lock)
            {
                if (_wasUsed)
                {
                    throw new InvalidOperationException("Should only used once");
                }
                _wasUsed = true;
            }
        }
    }

    private class CheckWithServiceDependency : IHealthCheck
    {
        public CheckWithServiceDependency(AnotherService _)
        {
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }

    private class CheckWithServiceNotMultiThreadDependency : IHealthCheck
    {
        private readonly CantBeMultiThreadedService _service;

        public CheckWithServiceNotMultiThreadDependency(CantBeMultiThreadedService service)
        {
            _service = service;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _service.Check();
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (InvalidOperationException e)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("failed", e));
            }

        }
    }

    private class NameCapturingCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>()
                {
                    { "name", context.Registration.Name },
                };
            return Task.FromResult(HealthCheckResult.Healthy(data: data));
        }
    }

    private class FailCapturingCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new Exception("check failed");
        }
    }

    private class DisposableDependeciesCheck : IHealthCheck
    {
        public DisposableDependeciesCheck(
            SynchronousDisposable disposable,
            AsyncOnlyDisposable asyncOnlyDisposable,
            SyncOrAsyncDisposable syncOrAsyncDisposable)
        {
            SynchronousDisposable = disposable;
            AsyncOnlyDisposable = asyncOnlyDisposable;
            SyncOrAsyncDisposable = syncOrAsyncDisposable;
        }

        public SynchronousDisposable SynchronousDisposable { get; }

        public AsyncOnlyDisposable AsyncOnlyDisposable { get; }

        public SyncOrAsyncDisposable SyncOrAsyncDisposable { get; }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(HealthCheckResult.Healthy(data: new Dictionary<string, object> { { "self", this } }));
        }
    }

    private class SynchronousDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("Dependency disposed multiple times.");
            }
            IsDisposed = true;
        }
    }

    private class AsyncOnlyDisposable : IAsyncDisposable
    {
        public bool IsAsyncDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            if (IsAsyncDisposed)
            {
                throw new InvalidOperationException("Dependency disposed multiple times.");
            }
            IsAsyncDisposed = true;
            return default;
        }
    }

    private class SyncOrAsyncDisposable : IDisposable, IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public bool IsAsyncDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed || IsAsyncDisposed)
            {
                throw new InvalidOperationException("Dependency disposed multiple times.");
            }
            IsDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            if (IsDisposed || IsAsyncDisposed)
            {
                throw new InvalidOperationException("Dependency disposed multiple times.");
            }
            IsAsyncDisposed = true;
            return default;
        }
    }
}
