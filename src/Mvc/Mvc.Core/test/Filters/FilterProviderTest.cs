// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class DefaultFilterProviderTest
{
    [Fact]
    public void DefaultFilterProvider_UsesFilter_WhenItsNotIFilterFactory()
    {
        // Arrange
        var filter = Mock.Of<IFilterMetadata>();

        var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filter, FilterScope.Global)),
            });

        var provider = CreateProvider();

        // Act
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var results = context.Results;

        // Assert
        var item = Assert.Single(results);
        Assert.Same(filter, item.Filter);
        Assert.Same(filter, item.Descriptor.Filter);
        Assert.Equal(0, item.Descriptor.Order);
    }

    [Fact]
    public void DefaultFilterProvider_UsesFilterFactory()
    {
        // Arrange
        var filter = Mock.Of<IFilterMetadata>();

        var filterFactory = new Mock<IFilterFactory>();
        filterFactory
            .Setup(ff => ff.CreateInstance(It.IsAny<IServiceProvider>()))
            .Returns(filter);

        var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filterFactory.Object, FilterScope.Global)),
            });

        var provider = CreateProvider();

        // Act
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        var results = context.Results;

        // Assert
        var item = Assert.Single(results);
        Assert.Same(filter, item.Filter);
        Assert.Same(filterFactory.Object, item.Descriptor.Filter);
        Assert.Equal(0, item.Descriptor.Order);
    }

    [Fact]
    public void DefaultFilterProvider_UsesFilterFactory_WithOrder()
    {
        // Arrange
        var filter = Mock.Of<IFilterMetadata>();

        var filterFactory = new Mock<IFilterFactory>();
        filterFactory
            .Setup(ff => ff.CreateInstance(It.IsAny<IServiceProvider>()))
            .Returns(filter);

        filterFactory.As<IOrderedFilter>().SetupGet(ff => ff.Order).Returns(17);

        var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filterFactory.Object, FilterScope.Global)),
            });

        var provider = CreateProvider();

        // Act
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);
        var results = context.Results;

        // Assert
        var item = Assert.Single(results);
        Assert.Same(filter, item.Filter);
        Assert.Same(filterFactory.Object, item.Descriptor.Filter);
        Assert.Equal(17, item.Descriptor.Order);
    }

    [Fact]
    public void DefaultFilterProvider_UsesFilterFactory_WithIFilterContainer()
    {
        // Arrange
        var filter = new Mock<IFilterContainer>();
        filter.SetupAllProperties();

        var filterFactory = new Mock<IFilterFactory>();
        filterFactory
            .Setup(ff => ff.CreateInstance(It.IsAny<IServiceProvider>()))
            .Returns(filter.As<IFilterMetadata>().Object);

        var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filterFactory.Object, FilterScope.Global)),
            });

        var provider = CreateProvider();

        // Act
        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);
        var results = context.Results;

        // Assert
        var item = Assert.Single(results);
        Assert.Same(filter.Object, item.Filter);
        Assert.Same(filterFactory.Object, ((IFilterContainer)item.Filter).FilterDefinition);
        Assert.Same(filterFactory.Object, item.Descriptor.Filter);
        Assert.Equal(0, item.Descriptor.Order);
    }

    private DefaultFilterProvider CreateProvider()
    {
        return new DefaultFilterProvider();
    }

    private FilterProviderContext CreateFilterContext(List<FilterItem> items)
    {
        var actionContext = CreateActionContext();
        actionContext.ActionDescriptor.FilterDescriptors = new List<FilterDescriptor>(
            items.Select(item => item.Descriptor));

        return new FilterProviderContext(actionContext, items);
    }

    private ActionContext CreateActionContext()
    {
        return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    }
}
