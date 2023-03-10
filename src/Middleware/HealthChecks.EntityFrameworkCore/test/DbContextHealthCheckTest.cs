// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public class DbContextHealthCheckTest
{
    // Just testing healthy here since it would be complicated to simulate a failure. All of that logic lives in EF anyway.
    [Fact]
    public async Task CheckAsync_DefaultTest_Healthy()
    {
        // Arrange
        var services = CreateServices();
        using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckAsync_CustomTest_Healthy()
    {
        // Arrange
        var services = CreateServices(async (c, ct) =>
        {
            return await c.Blogs.AnyAsync();
        });

        using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
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

    [Theory]
    [InlineData(HealthStatus.Degraded, HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    [InlineData(null, HealthStatus.Unhealthy)]
    public async Task CheckAsync_CustomTestWithFailureStatusSpecified_Expected(HealthStatus? specifiedFailureStatus, HealthStatus expectedStatus)
    {
        // Arrange
        var services = CreateServices(async (c, ct) =>
        {
            return await c.Blogs.AnyAsync();
        }, failureStatus: specifiedFailureStatus);

        using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

        // Assert
        Assert.Equal(expectedStatus, result.Status);
    }

    [Theory]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded)]
    [InlineData(HealthStatus.Healthy)]
    public async Task CheckAsync_CustomTestRegardlessOfFailureStatusSpecified_IfThrowsException_Unhealthy(HealthStatus healthStatus)
    {
        // Arrange
        const string exceptionMessage = "Something unexpected happened in the testQuery";

        var services = CreateServices(async (c, ct) =>
        {
            var any = await c.Blogs.AnyAsync();
            throw new Exception(exceptionMessage);
        }, failureStatus: healthStatus);

        using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        // var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        var registration = Assert.Single(services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        var check = ActivatorUtilities.CreateInstance<DbContextHealthCheck<TestDbContext>>(scope.ServiceProvider);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext() { Registration = registration, });

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(exceptionMessage, result.Description);
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
