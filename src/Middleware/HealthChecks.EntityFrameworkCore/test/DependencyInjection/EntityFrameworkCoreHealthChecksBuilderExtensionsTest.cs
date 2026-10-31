// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public class EntityFrameworkCoreHealthChecksBuilderExtensionsTest
{
    [Fact]
    public void AddDbContextCheck_RegistersDbContextHealthCheck()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("Test"));

        var builder = serviceCollection.AddHealthChecks();

        // Act
        builder.AddDbContextCheck<TestDbContext>("test", HealthStatus.Degraded, new[] { "tag1", "tag2", }, (c, ct) => Task.FromResult(true));

        // Assert
        var services = serviceCollection.BuildServiceProvider();

        var registrations = services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var registration = Assert.Single(registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Degraded, registration.FailureStatus);
        Assert.Equal(new[] { "tag1", "tag2", }, registration.Tags.ToArray());

        var options = services.GetRequiredService<IOptionsMonitor<DbContextHealthCheckOptions<TestDbContext>>>();
        Assert.NotNull(options.Get("test").CustomTestQuery);

        using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var check = Assert.IsType<DbContextHealthCheck<TestDbContext>>(registration.Factory(scope.ServiceProvider));
        }
    }
}
