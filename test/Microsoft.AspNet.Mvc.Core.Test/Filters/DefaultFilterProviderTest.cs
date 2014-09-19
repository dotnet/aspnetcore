// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Xunit;
using Moq;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class DefaultFilterProviderTest
    {
        [Fact]
        public void DefaultFilterProvider_UsesFilter_WhenItsNotIFilterFactory()
        {
            // Arrange
            var filter = Mock.Of<IFilter>();

            var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filter, FilterScope.Global)),
            });

            var provider = CreateProvider();

            // Act
            provider.Invoke(context, () => { });
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
            var filter = Mock.Of<IFilter>();

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
            provider.Invoke(context, () => { });
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
            var filter = Mock.Of<IFilter>();

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
            provider.Invoke(context, () => { });
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
                .Returns(filter.As<IFilter>().Object);

            var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filterFactory.Object, FilterScope.Global)),
            });

            var provider = CreateProvider();

            // Act
            provider.Invoke(context, () => { });
            var results = context.Results;

            // Assert
            var item = Assert.Single(results);
            Assert.Same(filter.Object, item.Filter);
            Assert.Same(filterFactory.Object, ((IFilterContainer)item.Filter).FilterDefinition);
            Assert.Same(filterFactory.Object, item.Descriptor.Filter);
            Assert.Equal(0, item.Descriptor.Order);
        }

        [Fact]
        public void DefaultFilterProvider_InsertsController_DefaultOrder()
        {
            // Arrange
            var filter1 = new Mock<IOrderedFilter>();
            filter1.SetupGet(f => f.Order).Returns(int.MaxValue);

            var filter2 = new Mock<IOrderedFilter>();
            filter2.SetupGet(f => f.Order).Returns(int.MaxValue);

            var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filter1.Object, FilterScope.Global)),
                new FilterItem(new FilterDescriptor(filter2.Object, FilterScope.Action)),
            });

            var controller = new Controller();
            context.ActionContext.Controller = controller;

            var provider = CreateProvider();

            // Act
            provider.Invoke(context, () => { });
            var results = context.Results;

            // Assert
            var controllerItem = results[1];
            Assert.Same(controller, controllerItem.Filter);
            Assert.Same(controller, controllerItem.Descriptor.Filter);
            Assert.Equal(FilterScope.Controller, controllerItem.Descriptor.Scope);
            Assert.Equal(Int32.MaxValue, controllerItem.Descriptor.Order);
        }

        [Fact]
        public void DefaultFilterProvider_InsertsController_CustomOrder()
        {
            // Arrange
            var filter1 = new Mock<IOrderedFilter>();
            filter1.SetupGet(f => f.Order).Returns(0);

            var filter2 = new Mock<IOrderedFilter>();
            filter2.SetupGet(f => f.Order).Returns(int.MaxValue);

            var context = CreateFilterContext(new List<FilterItem>()
            {
                new FilterItem(new FilterDescriptor(filter1.Object, FilterScope.Global)),
                new FilterItem(new FilterDescriptor(filter2.Object, FilterScope.Action)),
            });

            var controller = new Mock<IOrderedFilter>();
            controller.SetupGet(f => f.Order).Returns(17);

            context.ActionContext.Controller = controller.Object;

            var provider = CreateProvider();

            // Act
            provider.Invoke(context, () => { });
            var results = context.Results;

            // Assert
            var controllerItem = results[1];
            Assert.Same(controller.Object, controllerItem.Filter);
            Assert.Same(controller.Object, controllerItem.Descriptor.Filter);
            Assert.Equal(FilterScope.Controller, controllerItem.Descriptor.Scope);
            Assert.Equal(17, controllerItem.Descriptor.Order);
        }

        private DefaultFilterProvider CreateProvider()
        {
            var services = new ServiceContainer();

            return new DefaultFilterProvider(services);
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
}