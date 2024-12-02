// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DefaultPageActivatorProviderTest
{
    [Fact]
    public void CreateActivator_ThrowsIfPageTypeInfoIsNull()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor();
        var activator = new DefaultPageActivatorProvider();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => activator.CreateActivator(descriptor),
            "actionDescriptor",
            "The 'PageTypeInfo' property of 'actionDescriptor' must not be null.");
    }

    [Theory]
    [InlineData(typeof(TestPage))]
    [InlineData(typeof(PageWithMultipleConstructors))]
    public void CreateActivator_ReturnsFactoryForPage(Type type)
    {
        // Arrange
        var pageContext = new PageContext();
        var viewContext = new ViewContext();
        var descriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = type.GetTypeInfo(),
        };

        var activator = new DefaultPageActivatorProvider();

        // Act
        var factory = activator.CreateActivator(descriptor);
        var instance = factory(pageContext, viewContext);

        // Assert
        Assert.NotNull(instance);
        Assert.IsType(type, instance);
    }

    [Fact]
    public void CreateActivator_ThrowsIfTypeDoesNotHaveParameterlessConstructor()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = typeof(PageWithoutParameterlessConstructor).GetTypeInfo(),
        };
        var pageContext = new PageContext();
        var activator = new DefaultPageActivatorProvider();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => activator.CreateActivator(descriptor));
    }

    [Theory]
    [InlineData(typeof(TestPage))]
    [InlineData(typeof(object))]
    public void CreateReleaser_ReturnsNullForPagesThatDoNotImplementDisposable(Type pageType)
    {
        // Arrange
        var context = new PageContext();
        var activator = new DefaultPageActivatorProvider();
        var page = new TestPage();

        // Act
        var releaser = activator.CreateReleaser(new CompiledPageActionDescriptor
        {
            PageTypeInfo = pageType.GetTypeInfo()
        });

        // Assert
        Assert.Null(releaser);
    }

    [Theory]
    [InlineData(typeof(TestPage))]
    [InlineData(typeof(object))]
    public void CreateAsyncReleaser_ReturnsNullForPagesThatDoNotImplementDisposable(Type pageType)
    {
        // Arrange
        var context = new PageContext();
        var activator = new DefaultPageActivatorProvider();
        var page = new TestPage();

        // Act
        var releaser = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            PageTypeInfo = pageType.GetTypeInfo()
        });

        // Assert
        Assert.Null(releaser);
    }

    [Fact]
    public void CreateReleaser_CreatesDelegateThatDisposesDisposableTypes()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageActivatorProvider();
        var page = new DisposablePage();

        // Act & Assert
        var disposer = activator.CreateReleaser(new CompiledPageActionDescriptor
        {
            PageTypeInfo = page.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        disposer(context, viewContext, page);

        // Assert
        Assert.True(page.Disposed);
    }

    [Fact]
    public void CreateAsyncReleaser_CreatesDelegateThatDisposesDisposableTypes()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageActivatorProvider();
        var page = new DisposablePage();

        // Act & Assert
        var disposer = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            PageTypeInfo = page.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        disposer(context, viewContext, page);

        // Assert
        Assert.True(page.Disposed);
    }

    [Fact]
    public async Task CreateAsyncReleaser_CreatesDelegateThatDisposesAsyncDisposableTypes()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageActivatorProvider();
        var page = new AsyncDisposablePage();

        // Act & Assert
        var disposer = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            PageTypeInfo = page.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        await disposer(context, viewContext, page);

        // Assert
        Assert.True(page.Disposed);
    }

    [Fact]
    public async Task CreateAsyncReleaser_CreatesDelegateThatPrefersAsyncDisposeAsyncOverDispose()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageActivatorProvider();
        var page = new DisposableAndAsyncDisposablePage();

        // Act & Assert
        var disposer = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            PageTypeInfo = page.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        await disposer(context, viewContext, page);

        // Assert
        Assert.True(page.AsyncDisposed);
        Assert.False(page.SyncDisposed);
    }

    private class TestPage : Page
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class PageWithMultipleConstructors : Page
    {
        public PageWithMultipleConstructors(int x)
        {

        }

        public PageWithMultipleConstructors()
        {

        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class PageWithoutParameterlessConstructor : Page
    {
        public PageWithoutParameterlessConstructor(ILogger logger)
        {
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class DisposablePage : TestPage, IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private class AsyncDisposablePage : TestPage, IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }

    private class DisposableAndAsyncDisposablePage : TestPage, IDisposable, IAsyncDisposable
    {
        public bool AsyncDisposed { get; private set; }
        public bool SyncDisposed { get; private set; }

        public void Dispose()
        {
            SyncDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return default;
        }
    }
}
