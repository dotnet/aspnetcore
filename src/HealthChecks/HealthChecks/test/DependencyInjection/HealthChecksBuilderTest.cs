// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

// Integration tests for extension methods on IHealthCheckBuilder
//
// We test the longest overload of each 'family' of Add...Check methods, since they chain to each other.
public class HealthChecksBuilderTest
{
    [Fact]
    public void AddCheck_Instance()
    {
        // Arrange
        var instance = new DelegateHealthCheck((_) =>
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        });

        var services = CreateServices();
        services.AddHealthChecks().AddCheck("test", failureStatus: HealthStatus.Degraded, tags: new[] { "tag", }, instance: instance);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Degraded, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.Same(instance, registration.Factory(serviceProvider));
    }

    [Fact]
    public void AddCheck_T_TypeActivated()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks().AddCheck<TestHealthCheck>("test", failureStatus: HealthStatus.Degraded, tags: new[] { "tag", });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Degraded, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.IsType<TestHealthCheck>(registration.Factory(serviceProvider));
    }

    [Fact]
    public void AddCheck_T_Service()
    {
        // Arrange
        var instance = new TestHealthCheck();

        var services = CreateServices();
        services.AddSingleton(instance);
        services.AddHealthChecks().AddCheck<TestHealthCheck>("test", failureStatus: HealthStatus.Degraded, tags: new[] { "tag", });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Degraded, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.Same(instance, registration.Factory(serviceProvider));
    }

    [Fact]
    public void AddTypeActivatedCheck()
    {
        // Arrange
        var services = CreateServices();
        services
            .AddHealthChecks()
            .AddTypeActivatedCheck<TestHealthCheckWithArgs>("test", failureStatus: HealthStatus.Degraded, tags: new[] { "tag", }, args: new object[] { 5, "hi", });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Degraded, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);

        var check = Assert.IsType<TestHealthCheckWithArgs>(registration.Factory(serviceProvider));
        Assert.Equal(5, check.I);
        Assert.Equal("hi", check.S);
    }

    [Fact]
    public void AddDelegateCheck_NoArg()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks().AddCheck("test", tags: new[] { "tag", }, check: () =>
        {
            return HealthCheckResult.Healthy();
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.IsType<DelegateHealthCheck>(registration.Factory(serviceProvider));
    }

    [Fact]
    public void AddDelegateCheck_CancellationToken()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks().AddCheck("test", (_) =>
        {
            return HealthCheckResult.Degraded();
        }, tags: new[] { "tag", });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.IsType<DelegateHealthCheck>(registration.Factory(serviceProvider));
    }

    [Fact]
    public void AddAsyncDelegateCheck_NoArg()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks().AddAsyncCheck("test", () =>
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }, tags: new[] { "tag", });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.IsType<DelegateHealthCheck>(registration.Factory(serviceProvider));
    }

    [Fact]
    public void AddAsyncDelegateCheck_CancellationToken()
    {
        // Arrange
        var services = CreateServices();
        services.AddHealthChecks().AddAsyncCheck("test", (_) =>
        {
            return Task.FromResult(HealthCheckResult.Unhealthy());
        }, tags: new[] { "tag", });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(options.Registrations);
        Assert.Equal("test", registration.Name);
        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Equal<string>(new[] { "tag", }, registration.Tags);
        Assert.IsType<DelegateHealthCheck>(registration.Factory(serviceProvider));
    }

    [Fact]
    public void ChecksCanBeRegisteredInMultipleCallsToAddHealthChecks()
    {
        var services = new ServiceCollection();
        services
            .AddHealthChecks()
            .AddAsyncCheck("Foo", () => Task.FromResult(HealthCheckResult.Healthy()));
        services
            .AddHealthChecks()
            .AddAsyncCheck("Bar", () => Task.FromResult(HealthCheckResult.Healthy()));

        // Act
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        // Assert
        Assert.Collection(
            options.Value.Registrations,
            actual => Assert.Equal("Foo", actual.Name),
            actual => Assert.Equal("Bar", actual.Name));
    }

    private IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        return services;
    }

    private class TestHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }

    private class TestHealthCheckWithArgs : IHealthCheck
    {
        public TestHealthCheckWithArgs(int i, string s)
        {
            I = i;
            S = s;
        }

        public int I { get; set; }

        public string S { get; set; }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
