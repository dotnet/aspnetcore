// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class HealthCheckServiceTests
    {
        [Fact]
        public void Constructor_BuildsDictionaryOfChecks()
        {
            // Arrange
            var fooCheck = new HealthCheck("Foo", _ => Task.FromResult(HealthCheckResult.Healthy()));
            var barCheck = new HealthCheck("Bar", _ => Task.FromResult(HealthCheckResult.Healthy()));
            var bazCheck = new HealthCheck("Baz", _ => Task.FromResult(HealthCheckResult.Healthy()));
            var checks = new[] { fooCheck, barCheck, bazCheck };

            // Act
            var service = new HealthCheckService(checks);

            // Assert
            Assert.Same(fooCheck, service.Checks["Foo"]);
            Assert.Same(barCheck, service.Checks["Bar"]);
            Assert.Same(bazCheck, service.Checks["Baz"]);
            Assert.Equal(3, service.Checks.Count);
        }

        [Fact]
        public void Constructor_ThrowsUsefulExceptionForDuplicateNames()
        {
            // Arrange
            var checks = new[]
            {
                new HealthCheck("Foo", _ => Task.FromResult(HealthCheckResult.Healthy())),
                new HealthCheck("Foo", _ => Task.FromResult(HealthCheckResult.Healthy())),
                new HealthCheck("Bar", _ => Task.FromResult(HealthCheckResult.Healthy())),
                new HealthCheck("Baz", _ => Task.FromResult(HealthCheckResult.Healthy())),
                new HealthCheck("Baz", _ => Task.FromResult(HealthCheckResult.Healthy())),
            };

            // Act
            var exception = Assert.Throws<ArgumentException>(() => new HealthCheckService(checks));

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

            var healthyCheck = new HealthCheck("HealthyCheck", _ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage, data)));
            var degradedCheck = new HealthCheck("DegradedCheck", _ => Task.FromResult(HealthCheckResult.Degraded(DegradedMessage)));
            var unhealthyCheck = new HealthCheck("UnhealthyCheck", _ => Task.FromResult(HealthCheckResult.Unhealthy(UnhealthyMessage, exception)));

            var service = new HealthCheckService(new[]
            {
                healthyCheck,
                degradedCheck,
                unhealthyCheck,
            });

            // Act
            var results = await service.CheckHealthAsync();

            // Assert
            Assert.Collection(results.Results,
                actual =>
                {
                    Assert.Equal(healthyCheck.Name, actual.Key);
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
                    Assert.Equal(degradedCheck.Name, actual.Key);
                    Assert.Equal(DegradedMessage, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Degraded, actual.Value.Status);
                    Assert.Null(actual.Value.Exception);
                    Assert.Empty(actual.Value.Data);
                },
                actual =>
                {
                    Assert.Equal(unhealthyCheck.Name, actual.Key);
                    Assert.Equal(UnhealthyMessage, actual.Value.Description);
                    Assert.Equal(HealthCheckStatus.Unhealthy, actual.Value.Status);
                    Assert.Same(exception, actual.Value.Exception);
                    Assert.Empty(actual.Value.Data);
                });
        }

        [Fact]
        public async Task CheckAsync_RunsProvidedChecksAndAggregatesResultsAsync()
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

            var healthyCheck = new HealthCheck("HealthyCheck", _ => Task.FromResult(HealthCheckResult.Healthy(HealthyMessage, data)));
            var degradedCheck = new HealthCheck("DegradedCheck", _ => Task.FromResult(HealthCheckResult.Degraded(DegradedMessage)));
            var unhealthyCheck = new HealthCheck("UnhealthyCheck", _ => Task.FromResult(HealthCheckResult.Unhealthy(UnhealthyMessage, exception)));

            var service = new HealthCheckService(new[]
            {
                healthyCheck,
                degradedCheck,
                unhealthyCheck,
            });

            // Act
            var results = await service.CheckHealthAsync(new[]
            {
                service.Checks["HealthyCheck"]
            });

            // Assert
            Assert.Collection(results.Results,
                actual =>
                {
                    Assert.Equal(healthyCheck.Name, actual.Key);
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
            var service = new HealthCheckService(new[]
            {
                new HealthCheck("Throws", ct => throw thrownException),
                new HealthCheck("Faults", ct => Task.FromException<HealthCheckResult>(faultedException)),
                new HealthCheck("Succeeds", ct => Task.FromResult(HealthCheckResult.Healthy())),
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
            var service = new HealthCheckService(new[] { check }, loggerFactory.CreateLogger<HealthCheckService>());

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
            var service = new HealthCheckService(new[]
            {
                new HealthCheck("Kaboom", ct => Task.FromResult(default(HealthCheckResult))),
            });

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckHealthAsync());

            // Assert
            Assert.Equal("Health check 'Kaboom' returned a result with a status of Unknown", ex.Message);
        }
    }
}
