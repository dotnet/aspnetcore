// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseRateLimiter_ThrowsOnNullOptions()
    {
        var appBuilder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        Assert.Throws<ArgumentNullException>(() => appBuilder.UseRateLimiter(null));
    }

    [Fact]
    public void UseRateLimiter_RespectsOptions()
    {
        // These are the options that should get used
        var options = new RateLimiterOptions();
        options.DefaultRejectionStatusCode = 429;

        // These should not get used
        var services = new ServiceCollection();
        services.Configure<RateLimiterOptions>(options =>
        {
            options.DefaultRejectionStatusCode = 404;
        });
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        appBuilder.UseRateLimiter(options);
        var configuredOptions = appBuilder.ApplicationServices.GetRequiredService<RateLimiterOptions>();
        Assert.Equal(429, configuredOptions.DefaultRejectionStatusCode);
    }
}
