// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

public class DefaultHttpContextFactoryTests
{
    [Fact]
    public void CreateHttpContextSetsHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddOptions()
            .AddHttpContextAccessor()
            .BuildServiceProvider();
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var contextFactory = new DefaultHttpContextFactory(services);

        // Act
        var context = contextFactory.Create(new FeatureCollection());

        // Assert
        Assert.Same(context, accessor.HttpContext);
    }

    [Fact]
    public void DisposeHttpContextSetsHttpContextAccessorToNull()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddOptions()
            .AddHttpContextAccessor()
            .BuildServiceProvider();
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var contextFactory = new DefaultHttpContextFactory(services);

        // Act
        var context = contextFactory.Create(new FeatureCollection());

        // Assert
        Assert.Same(context, accessor.HttpContext);

        contextFactory.Dispose(context);

        Assert.Null(accessor.HttpContext);
    }

    [Fact]
    public void AllowsCreatingContextWithoutSettingAccessor()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider();
        var contextFactory = new DefaultHttpContextFactory(services);

        // Act & Assert
        var context = contextFactory.Create(new FeatureCollection());
        contextFactory.Dispose(context);
    }

    [Fact]
    public void SetsDefaultPropertiesOnHttpContext()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider();
        var contextFactory = new DefaultHttpContextFactory(services);

        // Act & Assert
        var context = contextFactory.Create(new FeatureCollection()) as DefaultHttpContext;
        Assert.NotNull(context);
        Assert.NotNull(context.FormOptions);
        Assert.NotNull(context.ServiceScopeFactory);

        Assert.Same(services.GetRequiredService<IServiceScopeFactory>(), context.ServiceScopeFactory);
    }

    [Fact]
    public void CreateHttpContextSetsActiveField()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider();
        var contextFactory = new DefaultHttpContextFactory(services);

        // Act & Assert
        var context = contextFactory.Create(new FeatureCollection()) as DefaultHttpContext;
        Assert.True(context._active);

        context.Uninitialize();

        Assert.False(context._active);
    }

    [Fact]
    public void InitializeHttpContextSetsActiveField()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider();
        var contextFactory = new DefaultHttpContextFactory(services);

        // Act & Assert
        var context = new DefaultHttpContext();
        contextFactory.Initialize(context, new FeatureCollection());
        Assert.True(context._active);

        context.Uninitialize();

        Assert.False(context._active);
    }
}
