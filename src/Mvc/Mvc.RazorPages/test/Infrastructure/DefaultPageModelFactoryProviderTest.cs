// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DefaultPageModelFactoryProviderTest
{
    [Fact]
    public void CreateModelFactory_ReturnsNullFactoryIfModelTypeIsNull()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor();
        var pageContext = new PageContext();
        var factoryProvider = CreateModelFactoryProvider();

        // Act
        var factory = factoryProvider.CreateModelFactory(descriptor);

        // Assert
        Assert.Null(factory);
    }

    [Fact]
    public void CreateModelDisposer_ReturnsNullFactoryIfModelTypeIsNull()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor();
        var pageContext = new PageContext();
        var factoryProvider = CreateModelFactoryProvider();

        // Act
        var disposer = factoryProvider.CreateModelDisposer(descriptor);

        // Assert
        Assert.Null(disposer);
    }

    [Fact]
    public void ModelFactory_InitializesModelInstances()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(SimpleModel).GetTypeInfo(),
        };
        var pageContext = new PageContext();
        var factoryProvider = CreateModelFactoryProvider();

        // Act
        var factory = factoryProvider.CreateModelFactory(descriptor);
        var instance = factory(pageContext);

        // Assert
        var model = Assert.IsType<SimpleModel>(instance);
        Assert.NotNull(model);
    }

    [Fact]
    public void ModelFactory_InjectsPropertiesWithPageContextAttribute()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(ModelWithPageContext).GetTypeInfo(),
        };
        var pageContext = new PageContext();
        var factoryProvider = CreateModelFactoryProvider();

        // Act
        var factory = factoryProvider.CreateModelFactory(descriptor);
        var instance = factory(pageContext);

        // Assert
        var testModel = Assert.IsType<ModelWithPageContext>(instance);
        Assert.Same(pageContext, testModel.ContextWithAttribute);
        Assert.Null(testModel.ContextWithoutAttribute);
    }

    [Fact]
    public void CreateModelDisposer_ReturnsDisposerFromModelActivatorProvider()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(SimpleModel).GetTypeInfo()
        };
        var pageContext = new PageContext();
        var modelActivatorProvider = new Mock<IPageModelActivatorProvider>();
        Action<PageContext, object> disposer = (_, __) => { };
        modelActivatorProvider.Setup(p => p.CreateReleaser(descriptor))
            .Returns(disposer);
        var factoryProvider = CreateModelFactoryProvider(modelActivatorProvider.Object);

        // Act
        var actual = factoryProvider.CreateModelDisposer(descriptor);

        // Assert
        Assert.Same(disposer, actual);
    }

    [Fact]
    public void CreateAsyncModelDisposer_ReturnsDisposerFromModelActivatorProvider()
    {
        // Arrange
        var descriptor = new CompiledPageActionDescriptor
        {
            ModelTypeInfo = typeof(SimpleModel).GetTypeInfo()
        };
        var pageContext = new PageContext();
        var modelActivatorProvider = new Mock<IPageModelActivatorProvider>();
        Func<PageContext, object, ValueTask> disposer = (_, __) => default;
        modelActivatorProvider.Setup(p => p.CreateAsyncReleaser(descriptor))
            .Returns(disposer);
        var factoryProvider = CreateModelFactoryProvider(modelActivatorProvider.Object);

        // Act
        var actual = factoryProvider.CreateAsyncModelDisposer(descriptor);

        // Assert
        Assert.Same(disposer, actual);
    }

    private static DefaultPageModelFactoryProvider CreateModelFactoryProvider(
        IPageModelActivatorProvider modelActivator = null)
    {
        if (modelActivator == null)
        {
            var mockActivator = new Mock<IPageModelActivatorProvider>();
            mockActivator.Setup(a => a.CreateActivator(It.IsAny<CompiledPageActionDescriptor>()))
                .Returns((CompiledPageActionDescriptor descriptor) =>
                {
                    return (context) => Activator.CreateInstance(descriptor.ModelTypeInfo.AsType());
                });

            modelActivator = mockActivator.Object;
        }

        return new DefaultPageModelFactoryProvider(modelActivator);
    }

    private class SimpleModel
    {
    }

    private class ModelWithPageContext
    {
        [PageContext]
        public PageContext ContextWithAttribute { get; set; }

        public PageContext ContextWithoutAttribute { get; set; }
    }
}
