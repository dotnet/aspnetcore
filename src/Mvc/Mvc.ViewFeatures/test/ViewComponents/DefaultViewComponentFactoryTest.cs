// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

public class DefaultViewComponentFactoryTest
{
    [Fact]
    public void CreateViewComponent_ActivatesProperties_OnTheInstance()
    {
        // Arrange
        var context = new ViewComponentContext
        {
        };

        var component = new ActivablePropertiesViewComponent();
        var activator = new Mock<IViewComponentActivator>();
        activator.Setup(a => a.Create(context))
            .Returns(component);

        var factory = new DefaultViewComponentFactory(activator.Object);

        // Act
        var result = factory.CreateViewComponent(context);

        // Assert
        var activablePropertiesComponent = Assert.IsType<ActivablePropertiesViewComponent>(result);

        Assert.Same(component, activablePropertiesComponent);
        Assert.Same(component.Context, activablePropertiesComponent.Context);
    }

    [Fact]
    public void ReleaseViewComponent_CallsDispose_OnTheInstance()
    {
        // Arrange
        var context = new ViewComponentContext
        {
        };

        var component = new ActivablePropertiesViewComponent();

        var viewComponentActivator = new Mock<IViewComponentActivator>();
        viewComponentActivator.Setup(vca => vca.Release(context, component))
            .Callback<ViewComponentContext, object>((c, o) => (o as IDisposable)?.Dispose());

        var factory = new DefaultViewComponentFactory(viewComponentActivator.Object);

        // Act
        factory.ReleaseViewComponent(context, component);

        // Assert
        Assert.True(component.Disposed);
    }

    [Fact]
    public async Task ReleaseViewComponentAsync_CallsDispose_OnTheInstance()
    {
        // Arrange
        var context = new ViewComponentContext
        {
        };

        var component = new ActivablePropertiesViewComponent();

        var viewComponentActivator = new Mock<IViewComponentActivator>();
        viewComponentActivator.Setup(vca => vca.ReleaseAsync(context, component))
            .Callback<ViewComponentContext, object>((c, o) => (o as IDisposable)?.Dispose())
            .Returns(default(ValueTask));

        var factory = new DefaultViewComponentFactory(viewComponentActivator.Object);

        // Act
        await factory.ReleaseViewComponentAsync(context, component);

        // Assert
        Assert.True(component.Disposed);
    }

    [Fact]
    public async Task ReleaseViewComponentAsync_CallsDisposeAsync_OnAsyncDisposableComponents()
    {
        // Arrange
        var context = new ViewComponentContext
        {
        };

        var component = new AsyncDisposableViewComponent();

        var viewComponentActivator = new Mock<IViewComponentActivator>();
        viewComponentActivator.Setup(vca => vca.ReleaseAsync(context, component))
            .Callback<ViewComponentContext, object>((c, o) => (o as IAsyncDisposable)?.DisposeAsync())
            .Returns(default(ValueTask));

        var factory = new DefaultViewComponentFactory(viewComponentActivator.Object);

        // Act
        await factory.ReleaseViewComponentAsync(context, component);

        // Assert
        Assert.True(component.Disposed);
    }
}

public class ActivablePropertiesViewComponent : IDisposable
{
    [ViewComponentContext]
    public ViewComponentContext Context { get; set; }

    public bool Disposed { get; private set; }

    public void Dispose()
    {
        Disposed = true;
    }

    public string Invoke()
    {
        return "something";
    }
}

public class AsyncDisposableViewComponent : IAsyncDisposable
{
    [ViewComponentContext]
    public ViewComponentContext Context { get; set; }

    public bool Disposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return default;
    }

    public string Invoke()
    {
        return "something";
    }
}

public class SyncAndAsyncDisposableViewComponent : IDisposable, IAsyncDisposable
{
    [ViewComponentContext]
    public ViewComponentContext Context { get; set; }

    public bool AsyncDisposed { get; private set; }
    public bool SyncDisposed { get; private set; }

    public void Dispose() => SyncDisposed = true;

    public ValueTask DisposeAsync()
    {
        AsyncDisposed = true;
        return default;
    }

    public string Invoke()
    {
        return "something";
    }
}
