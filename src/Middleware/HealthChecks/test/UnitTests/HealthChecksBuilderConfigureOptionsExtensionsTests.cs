// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.HealthChecks;

public class HealthChecksBuilderConfigureOptionsExtensionsTests
{
    [Fact]
    public void ConfigureHealthCheckOptions_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddHealthChecks().ConfigureHealthCheckOptions(options =>
        {
            options.AllowCachingResponses = true;
            options.ResultStatusCodes = new Dictionary<HealthStatus, int>
            {
                { HealthStatus.Healthy, 200 },
                { HealthStatus.Degraded, 201 },
                { HealthStatus.Unhealthy, 503 },
            };
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckOptions>>().Value;

        Assert.True(options.AllowCachingResponses);
        Assert.Equal(201, options.ResultStatusCodes[HealthStatus.Degraded]);
    }

    [Fact]
    public void ConfigureHealthCheckOptions_ReturnsSameBuilderForChaining()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        var returned = builder.ConfigureHealthCheckOptions(_ => { });

        Assert.Same(builder, returned);
    }

    [Fact]
    public void ConfigureHealthCheckOptions_ChainsWithAddCheck()
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .ConfigureHealthCheckOptions(options => options.AllowCachingResponses = true)
            .AddCheck("self", () => HealthCheckResult.Healthy());

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<HealthCheckService>());
        Assert.True(provider.GetRequiredService<IOptions<HealthCheckOptions>>().Value.AllowCachingResponses);
    }

    [Fact]
    public void ConfigureHealthCheckOptions_NullBuilder_Throws()
    {
        IHealthChecksBuilder builder = null!;
        Assert.Throws<ArgumentNullException>(
            () => builder.ConfigureHealthCheckOptions(_ => { }));
    }

    [Fact]
    public void ConfigureHealthCheckOptions_NullDelegate_Throws()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();

        Assert.Throws<ArgumentNullException>(
            () => builder.ConfigureHealthCheckOptions(null!));
    }

    [Fact]
    public void ConfigureHealthCheckOptions_IsAdditive()
    {
        var services = new ServiceCollection();

        services
            .AddHealthChecks()
            .ConfigureHealthCheckOptions(options => options.AllowCachingResponses = true)
            .ConfigureHealthCheckOptions(options =>
            {
                options.ResultStatusCodes = new Dictionary<HealthStatus, int>
                {
                    { HealthStatus.Healthy, 299 },
                    { HealthStatus.Degraded, 200 },
                    { HealthStatus.Unhealthy, 503 },
                };
            });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckOptions>>().Value;

        Assert.True(options.AllowCachingResponses);
        Assert.Equal(299, options.ResultStatusCodes[HealthStatus.Healthy]);
    }
}
