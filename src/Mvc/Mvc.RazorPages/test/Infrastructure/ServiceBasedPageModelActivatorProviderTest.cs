// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class ServiceBasedPageModelActivatorProviderTest
{
    [Fact]
    public void CreateActivator_ThrowsIfModelTypeInfoOnActionDescriptorIsNull()
    {
        // Arrange
        var activatorProvider = new ServiceBasedPageModelActivatorProvider();
        var descriptor = new CompiledPageActionDescriptor();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => activatorProvider.CreateActivator(descriptor),
            "descriptor",
            "The 'ModelTypeInfo' property of 'descriptor' must not be null.");
    }

    [Fact]
    public void Create_GetsServicesFromServiceProvider()
    {
        // Arrange
        var simpleModel = new DISimpleModel();
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(s => s.GetService(typeof(DISimpleModel)))
            .Returns(simpleModel)
            .Verifiable();

        var activatorProvider = new ServiceBasedPageModelActivatorProvider();
        var pageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object,
            },
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                ModelTypeInfo = typeof(DISimpleModel).GetTypeInfo(),
            }
        };

        // Act
        var activator = activatorProvider.CreateActivator(pageContext.ActionDescriptor);
        var instance = activator(pageContext);

        // Assert
        Assert.Same(simpleModel, instance);
        serviceProvider.Verify();
    }

    [Fact]
    public void CreateActivator_CreatesModelInstance()
    {
        // Arrange
        var simpleModel = new DISimpleModel();
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(s => s.GetService(typeof(DISimpleModel)))
            .Returns(simpleModel)
            .Verifiable();

        var activatorProvider = new ServiceBasedPageModelActivatorProvider();
        var pageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object,
            },
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                ModelTypeInfo = typeof(DISimpleModel).GetTypeInfo(),
            }
        };

        // Act
        var activator = activatorProvider.CreateActivator(pageContext.ActionDescriptor);
        var model = activator(pageContext);

        // Assert
        var simpleModel2 = Assert.IsType<DISimpleModel>(model);
        Assert.NotNull(simpleModel2);
    }

    [Fact]
    public void Create_ThrowsIfModelIsNotRegisteredInServiceProvider()
    {
        // Arrange
        var expected = "No service for type '" + typeof(DISimpleModel) + "' has been registered.";
        var model = new DISimpleModel();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = Mock.Of<IServiceProvider>()
        };

        var activatorProvider = new ServiceBasedPageModelActivatorProvider();
        var context = new PageContext
        {
            HttpContext = httpContext,
            ActionDescriptor = new CompiledPageActionDescriptor
            {
                ModelTypeInfo = typeof(DISimpleModel).GetTypeInfo(),
            }
        };

        // Act and Assert
        var activator = activatorProvider.CreateActivator(context.ActionDescriptor);
        var ex = Assert.Throws<InvalidOperationException>(
            () => activator(context));

        Assert.Equal(expected, ex.Message);
    }

    [Theory]
    [InlineData(typeof(SimpleModel))]
    [InlineData(typeof(object))]
    public void CreateReleaser_ReturnsNullForPageModels(Type pageType)
    {
        // Arrange
        var context = new PageContext();
        var activator = new ServiceBasedPageModelActivatorProvider();
        var actionDescriptor = new CompiledPageActionDescriptor
        {
            PageTypeInfo = pageType.GetTypeInfo(),
        };

        // Act
        var releaser = activator.CreateReleaser(actionDescriptor);

        // Assert
        Assert.Null(releaser);
    }

    private class SimpleModel
    {
    }

    private class DISimpleModel : SimpleModel
    {
    }
}
