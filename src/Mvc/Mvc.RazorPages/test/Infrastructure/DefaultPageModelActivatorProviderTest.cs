// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DefaultPageModelActivatorProviderTest
{
    [Fact]
    public void CreateActivator_ThrowsIfModelTypeInfoOnActionDescriptorIsNull()
    {
        // Arrange
        var activatorProvider = new DefaultPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => activatorProvider.CreateActivator(actionDescriptor),
            "actionDescriptor",
            "The 'ModelTypeInfo' property of 'actionDescriptor' must not be null.");
    }

    [Fact]
    public void CreateActivator_CreatesModelInstance()
    {
        // Arrange
        var activatorProvider = new DefaultPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(SimpleModel).GetTypeInfo(),
        };
        var serviceCollection = new ServiceCollection();
        var generator = Mock.Of<IHtmlGenerator>();
        serviceCollection.AddSingleton(generator);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceCollection.BuildServiceProvider(),
        };
        var pageContext = new PageContext
        {
            HttpContext = httpContext
        };

        // Act
        var activator = activatorProvider.CreateActivator(actionDescriptor);
        var model = activator(pageContext);

        // Assert
        var simpleModel = Assert.IsType<SimpleModel>(model);
        Assert.NotNull(simpleModel);
    }

    [Fact]
    public void CreateActivator_TypeActivatesModelType()
    {
        // Arrange
        var activatorProvider = new DefaultPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(ModelWithServices).GetTypeInfo(),
        };
        var serviceCollection = new ServiceCollection();
        var generator = Mock.Of<IHtmlGenerator>();
        serviceCollection.AddSingleton(generator);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceCollection.BuildServiceProvider(),
        };
        var pageContext = new PageContext
        {
            HttpContext = httpContext
        };

        // Act
        var activator = activatorProvider.CreateActivator(actionDescriptor);
        var model = activator(pageContext);

        // Assert
        var modelWithServices = Assert.IsType<ModelWithServices>(model);
        Assert.Same(generator, modelWithServices.Generator);
    }

    [Theory]
    [InlineData(typeof(SimpleModel))]
    [InlineData(typeof(object))]
    public void CreateReleaser_ReturnsNullForModelsThatDoNotImplementDisposable(Type pageType)
    {
        // Arrange
        var context = new PageContext();
        var activator = new DefaultPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = pageType.GetTypeInfo(),
        };

        // Act
        var releaser = activator.CreateReleaser(actionDescriptor);

        // Assert
        Assert.Null(releaser);
    }

    [Theory]
    [InlineData(typeof(SimpleModel))]
    [InlineData(typeof(object))]
    public void CreateAsyncReleaser_ReturnsNullForModelsThatDoNotImplementDisposable(Type pageType)
    {
        // Arrange
        var context = new PageContext();
        var activator = new DefaultPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = pageType.GetTypeInfo(),
        };

        // Act
        var releaser = activator.CreateAsyncReleaser(actionDescriptor);

        // Assert
        Assert.Null(releaser);
    }

    [Fact]
    public void CreateReleaser_CreatesDelegateThatDisposesDisposableTypes()
    {
        // Arrange
        var context = new PageContext();

        var activator = new DefaultPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(DisposableModel).GetTypeInfo(),
        };

        var model = new DisposableModel();

        // Act & Assert
        var releaser = activator.CreateReleaser(actionDescriptor);
        releaser(context, model);

        // Assert
        Assert.True(model.Disposed);
    }

    [Fact]
    public void CreateAsyncReleaser_CreatesDelegateThatDisposesDisposableTypes()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageModelActivatorProvider();
        var model = new DisposableModel();

        // Act & Assert
        var disposer = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            ModelTypeInfo = model.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        disposer(context, model);

        // Assert
        Assert.True(model.Disposed);
    }

    [Fact]
    public async Task CreateAsyncReleaser_CreatesDelegateThatDisposesAsyncDisposableTypes()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageModelActivatorProvider();
        var model = new AsyncDisposableModel();

        // Act & Assert
        var disposer = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            ModelTypeInfo = model.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        await disposer(context, model);

        // Assert
        Assert.True(model.Disposed);
    }

    [Fact]
    public async Task CreateAsyncReleaser_CreatesDelegateThatPrefersAsyncDisposeAsyncOverDispose()
    {
        // Arrange
        var context = new PageContext();
        var viewContext = new ViewContext();
        var activator = new DefaultPageModelActivatorProvider();
        var model = new DisposableAndAsyncDisposableModel();

        // Act & Assert
        var disposer = activator.CreateAsyncReleaser(new CompiledPageActionDescriptor
        {
            ModelTypeInfo = model.GetType().GetTypeInfo()
        });
        Assert.NotNull(disposer);
        await disposer(context, model);

        // Assert
        Assert.True(model.AsyncDisposed);
        Assert.False(model.SyncDisposed);
    }

    private class SimpleModel
    {
    }

    private class ModelWithServices
    {
        public ModelWithServices(IHtmlGenerator generator)
        {
            Generator = generator;
        }

        public IHtmlGenerator Generator { get; }
    }

    private class DisposableModel : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private class AsyncDisposableModel : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }

    private class DisposableAndAsyncDisposableModel : IDisposable, IAsyncDisposable
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
