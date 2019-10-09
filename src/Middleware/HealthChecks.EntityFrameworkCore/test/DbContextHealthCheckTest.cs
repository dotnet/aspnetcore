// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public class DbContextHealthCheckTest
    {
        // Just testing healthy here since it would be complicated to simulate a failure. All of that logic lives in EF anyway.
        [Fact]
        public async Task CheckAsync_DefaultTest_Healthy()
        {
            // Arrange
            var services = CreateServices();
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
                var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

                // Act
                var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

                // Assert
                Assert.Equal(HealthStatus.Healthy, result.Status);
            }
        }

        [Fact]
        public async Task CheckAsync_CustomTest_Healthy()
        {
            // Arrange
            var services = CreateServices(async (c, ct) =>
            {
                return 0 < await c.Blogs.CountAsync();
            });

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
                var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

                // Add a blog so that the custom test passes
                var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                context.Add(new Blog());
                await context.SaveChangesAsync();

                // Act
                var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

                // Assert
                Assert.Equal(HealthStatus.Healthy, result.Status);
            }
        }

        [Fact]
        public async Task CheckAsync_CustomTestWithDegradedFailureStatusSpecified_Degraded()
        {
            // Arrange
            var services = CreateServices(async (c, ct) =>
            {
                return 0 < await c.Blogs.CountAsync();
            }, failureStatus: HealthStatus.Degraded);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
                var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

                // Act
                var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

                // Assert
                Assert.Equal(HealthStatus.Degraded, result.Status);
            }
        }

        [Fact]
        public async Task CheckAsync_CustomTestWithUnhealthyFailureStatusSpecified_Unhealthy()
        {
            // Arrange
            var services = CreateServices(async (c, ct) =>
            {
                return 0 < await c.Blogs.CountAsync();
            }, failureStatus: HealthStatus.Unhealthy);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
                var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

                // Act
                var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

                // Assert
                Assert.Equal(HealthStatus.Unhealthy, result.Status);
            }
        }

        [Fact]
        public async Task CheckAsync_CustomTestWithNoFailureStatusSpecified_Unhealthy()
        {
            // Arrange
            var services = CreateServices(async (c, ct) =>
            {
                return 0 < await c.Blogs.CountAsync();
            }, failureStatus: null);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
                var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

                // Act
                var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

                // Assert
                Assert.Equal(HealthStatus.Unhealthy, result.Status);
            }
        }

        // used to ensure each test uses a unique in-memory database
        private static int _testDbCounter;

        private static IServiceProvider CreateServices(
            Func<TestDbContext, CancellationToken, Task<bool>> testQuery = null,
            HealthStatus? failureStatus = HealthStatus.Unhealthy)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("Test" + Interlocked.Increment(ref _testDbCounter)));

            var builder = serviceCollection.AddHealthChecks();
            builder.AddDbContextCheck<TestDbContext>("test", failureStatus, new[] { "tag1", "tag2", }, testQuery);
            return serviceCollection.BuildServiceProvider();
        }
    }
}
