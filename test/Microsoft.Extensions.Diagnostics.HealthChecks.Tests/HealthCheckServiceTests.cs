// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class HealthCheckServiceTests
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
                .AddCheck(new HealthCheck("Foo", _ => Task.FromResult(HealthCheckResult.Healthy())))
                .AddCheck(new HealthCheck("Foo", _ => Task.FromResult(HealthCheckResult.Healthy())))
                .AddCheck(new HealthCheck("Bar", _ => Task.FromResult(HealthCheckResult.Healthy())))
                .AddCheck(new HealthCheck("Baz", _ => Task.FromResult(HealthCheckResult.Healthy())))
                .AddCheck(new HealthCheck("Baz", _ => Task.FromResult(HealthCheckResult.Healthy())));

            var services = serviceCollection.BuildServiceProvider();

            var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
            var logger = services.GetRequiredService<ILogger<HealthCheckService>>();

            // Act
            var exception = Assert.Throws<ArgumentException>(() => new HealthCheckService(scopeFactory, logger));

            // Assert
            Assert.Equal($"Duplicate health checks were registered with the name(s): Foo, Baz{Environment.NewLine}Parameter name: healthChecks", exception.Message);
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

            // Arrange
            var data = new Dictionary<string, object>()
            {
                { DataKey, DataValue }
            };

            var service = CreateHealthChecksService(b =>
            {
                b.AddCheck("HealthyCheck", _ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage, data)));
                b.AddCheck("DegradedCheck", _ => Task.FromResult(HealthCheckResult.Degraded(DegradedMessage)));
                b.AddCheck("UnhealthyCheck", _ => Task.FromResult(HealthCheckResult.Unhealthy(UnhealthyMessage, exception)));
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(results.Results,
                actual =>
                {
                    Assert.Equal("HealthyCheck", actual.Key);
                    Assert.Equal(HealthyMessage, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
                    Assert.Null(actual.Value.Exception);
                    Assert.Collection(actual.Value.Data, item =>
                    {
                        Assert.Equal(DataKey, item.Key);
                        Assert.Equal(DataValue, item.Value);
                    });
                },
                actual =>
                {
                    Assert.Equal("DegradedCheck", actual.Key);
                    Assert.Equal(DegradedMessage, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Degraded, actual.Value.Status);
                    Assert.Null(actual.Value.Exception);
                    Assert.Empty(actual.Value.Data);
                },
                actual =>
                {
                    Assert.Equal("UnhealthyCheck", actual.Key);
                    Assert.Equal(UnhealthyMessage, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Unhealthy, actual.Value.Status);
                    Assert.Same(exception, actual.Value.Exception);
                    Assert.Empty(actual.Value.Data);
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
                b.AddCheck("HealthyCheck", _ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage, data)));
                b.AddCheck("DegradedCheck", _ => Task.FromResult(HealthCheckResult.Degraded(DegradedMessage)));
                b.AddCheck("UnhealthyCheck", _ => Task.FromResult(HealthCheckResult.Unhealthy(UnhealthyMessage, exception)));
            });

            // Act
            var results = await service.CheckHealthAsync(c => c.Name == "HealthyCheck");

            // Assert
            Assert.Collection(results.Results,
                actual =>
                {
                    Assert.Equal("HealthyCheck", actual.Key);
                    Assert.Equal(HealthyMessage, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
                    Assert.Null(actual.Value.Exception);
                    Assert.Collection(actual.Value.Data, item =>
                    {
                        Assert.Equal(DataKey, item.Key);
                        Assert.Equal(DataValue, item.Value);
                    });
                });
        }

        [Fact]
        public async Task CheckHealthAsync_ConvertsExceptionInHealthCheckerToFailedResultAsync()
        {
            // Arrange
            var thrownException = new InvalidOperationException("Whoops!");
            var faultedException = new InvalidOperationException("Ohnoes!");

            var service = CreateHealthChecksService(b =>
            {
                b.AddCheck("Throws", ct => throw thrownException);
                b.AddCheck("Faults", ct => Task.FromException<HealthCheckResult>(faultedException));
                b.AddCheck("Succeeds", ct => Task.FromResult(HealthCheckResult.Healthy()));
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(results.Results,
                actual =>
                {
                    Assert.Equal("Throws", actual.Key);
                    Assert.Equal(thrownException.Message, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Failed, actual.Value.Status);
                    Assert.Same(thrownException, actual.Value.Exception);
                },
                actual =>
                {
                    Assert.Equal("Faults", actual.Key);
                    Assert.Equal(faultedException.Message, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Failed, actual.Value.Status);
                    Assert.Same(faultedException, actual.Value.Exception);
                },
                actual =>
                {
                    Assert.Equal("Succeeds", actual.Key);
                    Assert.Empty(actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
                    Assert.Null(actual.Value.Exception);
                });
        }

        [Fact]
        public async Task CheckHealthAsync_SetsUpALoggerScopeForEachCheck()
        {
            // Arrange
            var sink = new TestSink();
            var check = new HealthCheck("TestScope", cancellationToken =>
            {
                Assert.Collection(sink.Scopes,
                    actual =>
                    {
                        Assert.Equal(actual.LoggerName, typeof(HealthCheckService).FullName);
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

                b.AddCheck(check);
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(results.Results, actual =>
            {
                Assert.Equal("TestScope", actual.Key);
                Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
            });
        }

        [Fact]
        public async Task CheckHealthAsync_ThrowsIfCheckReturnsUnknownStatusResult()
        {
            // Arrange
            var service = CreateHealthChecksService(b =>
            {
                b.AddCheck("Kaboom", ct => Task.FromResult(default(HealthCheckResult)));
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckHealthAsync());

            // Assert
            Assert.Equal("Health check 'Kaboom' returned a result with a status of Unknown", ex.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_CheckCanDependOnTransientService()
        {
            // Arrange
            var service = CreateHealthChecksService(b =>
            {
                b.Services.AddTransient<AnotherService>();

                b.AddCheck<CheckWithServiceDependency>();
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(
                results.Results,
                actual =>
                {
                    Assert.Equal("Test", actual.Key);
                    Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
                });
        }

        [Fact]
        public async Task CheckHealthAsync_CheckCanDependOnScopedService()
        {
            // Arrange
            var service = CreateHealthChecksService(b =>
            {
                b.Services.AddScoped<AnotherService>();

                b.AddCheck<CheckWithServiceDependency>();
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(
                results.Results,
                actual =>
                {
                    Assert.Equal("Test", actual.Key);
                    Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
                });
        }

        [Fact]
        public async Task CheckHealthAsync_CheckCanDependOnSingletonService()
        {
            // Arrange
            var service = CreateHealthChecksService(b =>
            {
                b.Services.AddSingleton<AnotherService>();

                b.AddCheck<CheckWithServiceDependency>();
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(
                results.Results,
                actual =>
                {
                    Assert.Equal("Test", actual.Key);
                    Assert.Equal(HealthCheckStatus.Healthy, actual.Value.Status);
                });
        }

        private static HealthCheckService CreateHealthChecksService(Action<IHealthChecksBuilder> configure)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();

            var builder = services.AddHealthChecks();
            if (configure != null)
            {
                configure(builder);
            }

            return (HealthCheckService)services.BuildServiceProvider(validateScopes: true).GetRequiredService<IHealthCheckService>();
        }

        private class AnotherService { }

        private class CheckWithServiceDependency : IHealthCheck
        {
            public CheckWithServiceDependency(AnotherService _)
            {
            }

            public string Name => "Test";

            public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }
        }
    }
}
