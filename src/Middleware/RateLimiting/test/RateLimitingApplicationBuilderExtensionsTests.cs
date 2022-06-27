// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingApplicationBuilderExtensionsTests : LoggedTest
{

    [Fact]
    public void UseRateLimiter_ThrowsOnNullAppBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => RateLimitingApplicationBuilderExtensions.UseRateLimiter(null));
    }

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
        options.Limiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));

        // These should not get used
        var services = new ServiceCollection();
        services.Configure<RateLimiterOptions>(options =>
        {
            options.Limiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
            options.DefaultRejectionStatusCode = 404;
        });
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        appBuilder.UseRateLimiter(options);
        var app = appBuilder.Build();
        var context = new DefaultHttpContext();
        app.Invoke(context);
        Assert.Equal(429, context.Response.StatusCode);
    }
}
