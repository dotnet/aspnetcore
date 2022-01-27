// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

public class DefaultViewComponentActivatorTests
{
    [Fact]
    public void DefaultViewComponentActivator_ActivatesViewComponentContext()
    {
        // Arrange
        var expectedInstance = new TestViewComponent();

        var typeActivator = new Mock<ITypeActivatorCache>();
        typeActivator
            .Setup(ta => ta.CreateInstance<object>(It.IsAny<IServiceProvider>(), It.IsAny<Type>()))
            .Returns(expectedInstance);

        var activator = new DefaultViewComponentActivator(typeActivator.Object);

        var context = CreateContext(typeof(TestViewComponent));
        expectedInstance.ViewComponentContext = context;

        // Act
        var instance = activator.Create(context) as ViewComponent;

        // Assert
        Assert.NotNull(instance);
        Assert.Same(context, instance.ViewComponentContext);
    }

    [Fact]
    public void DefaultViewComponentActivator_ActivatesViewComponentContext_IgnoresNonPublic()
    {
        // Arrange
        var expectedInstance = new VisibilityViewComponent();

        var typeActivator = new Mock<ITypeActivatorCache>();
        typeActivator
            .Setup(ta => ta.CreateInstance<object>(It.IsAny<IServiceProvider>(), It.IsAny<Type>()))
            .Returns(expectedInstance);

        var activator = new DefaultViewComponentActivator(typeActivator.Object);

        var context = CreateContext(typeof(VisibilityViewComponent));
        expectedInstance.ViewComponentContext = context;

        // Act
        var instance = activator.Create(context) as VisibilityViewComponent;

        // Assert
        Assert.NotNull(instance);
        Assert.Same(context, instance.ViewComponentContext);
        Assert.Null(instance.C);
    }

    [Fact]
    public async Task DefaultViewComponentActivator_ReleaseAsync_PrefersAsyncDisposableOverDisposable()
    {
        // Arrange
        var instance = new SyncAndAsyncDisposableViewComponent();

        var activator = new DefaultViewComponentActivator(Mock.Of<ITypeActivatorCache>());

        var context = CreateContext(typeof(SyncAndAsyncDisposableViewComponent));

        // Act
        await activator.ReleaseAsync(context, instance);

        // Assert
        Assert.True(instance.AsyncDisposed);
        Assert.False(instance.SyncDisposed);
    }

    private static ViewComponentContext CreateContext(Type componentType)
    {
        return new ViewComponentContext
        {
            ViewComponentDescriptor = new ViewComponentDescriptor
            {
                TypeInfo = componentType.GetTypeInfo()
            },
            ViewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = Mock.Of<IServiceProvider>()
                }
            }
        };
    }

    private class TestViewComponent : ViewComponent
    {
        public Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class VisibilityViewComponent : ViewComponent
    {
        [ViewComponentContext]
        protected internal ViewComponentContext C { get; set; }
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
}
