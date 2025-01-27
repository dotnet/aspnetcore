// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Authorization.Test;

public class AuthorizationMetricsTest
{
    [Fact]
    public async Task Authorize_WithPolicyName_Success()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var authorizationService = BuildAuthorizationService(meterFactory);
        var meter = meterFactory.Meters.Single();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("Permission", "CanViewPage")], authenticationType: "someAuthentication"));

        using var authorizedRequestsCollector = new MetricCollector<long>(meterFactory, AuthorizationMetrics.MeterName, "aspnetcore.authorization.attempts");

        // Act
        await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.Equal(AuthorizationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authorizedRequestsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("Basic", (string)measurement.Tags["aspnetcore.authorization.policy"]);
        Assert.Equal("success", (string)measurement.Tags["aspnetcore.authorization.result"]);
        Assert.True((bool)measurement.Tags["user.is_authenticated"]);
    }

    [Fact]
    public async Task Authorize_WithPolicyName_Failure()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var authorizationService = BuildAuthorizationService(meterFactory);
        var meter = meterFactory.Meters.Single();
        var user = new ClaimsPrincipal(new ClaimsIdentity([])); // Will fail due to missing required claim

        using var authorizedRequestsCollector = new MetricCollector<long>(meterFactory, AuthorizationMetrics.MeterName, "aspnetcore.authorization.attempts");

        // Act
        await authorizationService.AuthorizeAsync(user, "Basic");

        // Assert
        Assert.Equal(AuthorizationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authorizedRequestsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("Basic", (string)measurement.Tags["aspnetcore.authorization.policy"]);
        Assert.Equal("failure", (string)measurement.Tags["aspnetcore.authorization.result"]);
        Assert.False((bool)measurement.Tags["user.is_authenticated"]);
    }

    [Fact]
    public async Task Authorize_WithPolicyName_PolicyNotFound()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var authorizationService = BuildAuthorizationService(meterFactory);
        var meter = meterFactory.Meters.Single();
        var user = new ClaimsPrincipal(new ClaimsIdentity([])); // Will fail due to missing required claim

        using var authorizedRequestsCollector = new MetricCollector<long>(meterFactory, AuthorizationMetrics.MeterName, "aspnetcore.authorization.attempts");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => authorizationService.AuthorizeAsync(user, "UnknownPolicy"));

        // Assert
        Assert.Equal(AuthorizationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authorizedRequestsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("UnknownPolicy", (string)measurement.Tags["aspnetcore.authorization.policy"]);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
        Assert.False((bool)measurement.Tags["user.is_authenticated"]);
        Assert.False(measurement.Tags.ContainsKey("aspnetcore.authorization.result"));
    }

    [Fact]
    public async Task Authorize_WithoutPolicyName_Success()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var authorizationService = BuildAuthorizationService(meterFactory, services =>
        {
            services.AddSingleton<IAuthorizationHandler>(new AlwaysHandler(succeed: true));
        });
        var meter = meterFactory.Meters.Single();
        var user = new ClaimsPrincipal(new ClaimsIdentity([]));

        using var authorizedRequestsCollector = new MetricCollector<long>(meterFactory, AuthorizationMetrics.MeterName, "aspnetcore.authorization.attempts");

        // Act
        await authorizationService.AuthorizeAsync(user, resource: null, new TestRequirement());

        // Assert
        Assert.Equal(AuthorizationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authorizedRequestsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("success", (string)measurement.Tags["aspnetcore.authorization.result"]);
        Assert.False((bool)measurement.Tags["user.is_authenticated"]);
        Assert.False(measurement.Tags.ContainsKey("aspnetcore.authorization.policy"));
    }

    [Fact]
    public async Task Authorize_WithoutPolicyName_Failure()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var authorizationService = BuildAuthorizationService(meterFactory); // Will fail because there is no handler registered
        var meter = meterFactory.Meters.Single();
        var user = new ClaimsPrincipal(new ClaimsIdentity([]));

        using var authorizedRequestsCollector = new MetricCollector<long>(meterFactory, AuthorizationMetrics.MeterName, "aspnetcore.authorization.attempts");

        // Act
        await authorizationService.AuthorizeAsync(user, resource: null, new TestRequirement());

        // Assert
        Assert.Equal(AuthorizationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authorizedRequestsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("failure", (string)measurement.Tags["aspnetcore.authorization.result"]);
        Assert.False((bool)measurement.Tags["user.is_authenticated"]);
        Assert.False(measurement.Tags.ContainsKey("aspnetcore.authorization.policy"));
    }

    [Fact]
    public async Task Authorize_WithoutPolicyName_ExceptionThrownInHandler()
    {
        // Arrange
        var meterFactory = new TestMeterFactory();
        var authorizationService = BuildAuthorizationService(meterFactory, services =>
        {
            services.AddSingleton<IAuthorizationHandler>(new AlwaysThrowHandler());
        });
        var meter = meterFactory.Meters.Single();
        var user = new ClaimsPrincipal(new ClaimsIdentity([]));

        using var authorizedRequestsCollector = new MetricCollector<long>(meterFactory, AuthorizationMetrics.MeterName, "aspnetcore.authorization.attempts");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => authorizationService.AuthorizeAsync(user, resource: null, new TestRequirement()));

        // Assert
        Assert.Equal("An error occurred in the authorization handler", ex.Message);
        Assert.Equal(AuthorizationMetrics.MeterName, meter.Name);
        Assert.Null(meter.Version);

        var measurement = Assert.Single(authorizedRequestsCollector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal("System.InvalidOperationException", (string)measurement.Tags["error.type"]);
        Assert.False((bool)measurement.Tags["user.is_authenticated"]);
        Assert.False(measurement.Tags.ContainsKey("aspnetcore.authorization.policy"));
        Assert.False(measurement.Tags.ContainsKey("aspnetcore.authorization.result"));
    }

    private static IAuthorizationService BuildAuthorizationService(TestMeterFactory meterFactory, Action<IServiceCollection> setupServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new AuthorizationMetrics(meterFactory));
        services.AddAuthorizationBuilder()
            .AddPolicy("Basic", policy => policy.RequireClaim("Permission", "CanViewPage"));
        services.AddLogging();
        services.AddOptions();
        setupServices?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private sealed class AlwaysHandler(bool succeed) : AuthorizationHandler<TestRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TestRequirement requirement)
        {
            if (succeed)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysThrowHandler : AuthorizationHandler<TestRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TestRequirement requirement)
        {
            throw new InvalidOperationException("An error occurred in the authorization handler");
        }
    }

    private sealed class TestRequirement : IAuthorizationRequirement;
}
