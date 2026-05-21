// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddHealthChecks_RegistersSingletonHealthCheckServiceIdempotently()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks();
        services.AddHealthChecks();

        // Assert
        Assert.Collection(services.OrderBy(s => s.ServiceType.FullName),
            actual =>
            {
                Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                Assert.Equal(typeof(HealthCheckService), actual.ServiceType);
                Assert.Equal(typeof(DefaultHealthCheckService), actual.ImplementationType);
                Assert.Null(actual.ImplementationInstance);
                Assert.Null(actual.ImplementationFactory);
            },
            actual =>
            {
                Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                Assert.Equal(typeof(IHostedService), actual.ServiceType);
                Assert.Equal(typeof(HealthCheckPublisherHostedService), actual.ImplementationType);
                Assert.Null(actual.ImplementationInstance);
                Assert.Null(actual.ImplementationFactory);
            });
    }

    [Fact] // see: https://github.com/dotnet/extensions/issues/639
    public void AddHealthChecks_RegistersPublisherService_WhenOtherHostedServicesRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton<IHostedService, DummyHostedService>();
        services.AddHealthChecks();

        // Assert
        Assert.Collection(services.OrderBy(s => s.ServiceType.FullName).ThenBy(s => s.ImplementationType!.FullName),
            actual =>
            {
                Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                Assert.Equal(typeof(HealthCheckService), actual.ServiceType);
                Assert.Equal(typeof(DefaultHealthCheckService), actual.ImplementationType);
                Assert.Null(actual.ImplementationInstance);
                Assert.Null(actual.ImplementationFactory);
            },
            actual =>
            {
                Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                Assert.Equal(typeof(IHostedService), actual.ServiceType);
                Assert.Equal(typeof(DummyHostedService), actual.ImplementationType);
                Assert.Null(actual.ImplementationInstance);
                Assert.Null(actual.ImplementationFactory);
            },
            actual =>
            {
                Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                Assert.Equal(typeof(IHostedService), actual.ServiceType);
                Assert.Equal(typeof(HealthCheckPublisherHostedService), actual.ImplementationType);
                Assert.Null(actual.ImplementationInstance);
                Assert.Null(actual.ImplementationFactory);
            });
    }

    [Fact]
    public void AddHealthChecks_WithConfigureOptions_InvokesDelegateOnResolvedOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var marker = new HealthCheckRegistration("marker", _ => Task.FromResult(HealthCheckResult.Healthy()), failureStatus: null, tags: null);

        // Act
        services.AddHealthChecks(options => options.Registrations.Add(marker));

        // Assert
        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        Assert.Same(marker, Assert.Single(resolved.Registrations));
    }

    [Fact]
    public void AddHealthChecks_WithConfigureOptions_ReturnsBuilderUsableForFurtherRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var fromOptions = new HealthCheckRegistration("from-options", _ => Task.FromResult(HealthCheckResult.Healthy()), failureStatus: null, tags: null);
        var fromBuilder = new HealthCheckRegistration("from-builder", _ => Task.FromResult(HealthCheckResult.Healthy()), failureStatus: null, tags: null);

        // Act
        var builder = services.AddHealthChecks(options => options.Registrations.Add(fromOptions));
        builder.Add(fromBuilder);

        // Assert
        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        Assert.Equal(new[] { "from-options", "from-builder" }, resolved.Registrations.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void AddHealthChecks_WithConfigureOptions_ThrowsForNullServices()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddHealthChecks(_ => { }));
    }

    [Fact]
    public void AddHealthChecks_WithConfigureOptions_ThrowsForNullConfigureDelegate()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddHealthChecks(configureOptions: null!));
    }

    private class DummyHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
