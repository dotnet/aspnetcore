// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingApplicationBuilderExtensionsTests : LoggedTest
{
    [Fact]
    public void UseRateLimiter_ThrowsOnNullAppBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => RateLimiterApplicationBuilderExtensions.UseRateLimiter(null));
    }

    [Fact]
    public void UseRateLimiter_ThrowsOnNullOptions()
    {
        var appBuilder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        Assert.Throws<ArgumentNullException>(() => appBuilder.UseRateLimiter(null));
    }

    [Fact]
    public void UseRateLimiter_RequireServices()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => appBuilder.UseRateLimiter());
        Assert.Equal("Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddRateLimiter' in the application startup code.", ex.Message);
    }

    [Fact]
    public void UseRateLimiter_RespectsOptions()
    {
        // These are the options that should get used
        var options = new RateLimiterOptions();
        options.RejectionStatusCode = 429;
        options.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));

        // These should not get used
        var services = new ServiceCollection();
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
            options.RejectionStatusCode = 404;
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

    [Fact]
    public async Task UseRateLimiter_DoNotThrowWithoutOptions()
    {
        var services = new ServiceCollection();
        services.AddRateLimiter();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        appBuilder.UseRateLimiter();
        var app = appBuilder.Build();
        var context = new DefaultHttpContext();
        var exception = await Record.ExceptionAsync(() => app.Invoke(context));

        // Assert
        Assert.Null(exception);
    }
}
