// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.Internal
{
    public class FilterCacheTest
    {
        [Fact]
        public void GetFilters_CachesAllFilters()
        {
            // Arrange
            var services = CreateServices();
            var cache = CreateCache(new DefaultFilterProvider());

            var action = new ControllerActionDescriptor()
            {
                FilterDescriptors = new[]
                {
                    new FilterDescriptor(new TestFilter(), FilterScope.Action),
                    new FilterDescriptor(new TestFilter(), FilterScope.Action),
                },
            };

            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), action);

            // Act - 1
            var filters1 = cache.GetFilters(context);

            // Assert - 1
            Assert.Collection(
                filters1,
                f => Assert.Same(action.FilterDescriptors[0].Filter, f), // Copied by provider
                f => Assert.Same(action.FilterDescriptors[1].Filter, f)); // Copied by provider

            // Act - 2
            var filters2 = cache.GetFilters(context);

            Assert.Same(filters1, filters2);

            Assert.Collection(
                filters2,
                f => Assert.Same(action.FilterDescriptors[0].Filter, f), // Cached
                f => Assert.Same(action.FilterDescriptors[1].Filter, f)); // Cached
        }

        [Fact]
        public void GetFilters_CachesFilterFromFactory()
        {
            // Arrange
            var services = CreateServices();
            var cache = CreateCache(new DefaultFilterProvider());

            var action = new ControllerActionDescriptor()
            {
                FilterDescriptors = new[]
                {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = true }, FilterScope.Action),
                    new FilterDescriptor(new TestFilter(), FilterScope.Action),
                },
            };

            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), action);

            // Act - 1
            var filters1 = cache.GetFilters(context);

            // Assert - 1
            Assert.Collection(
                filters1,
                f => Assert.NotSame(action.FilterDescriptors[0].Filter, f), // Created by factory
                f => Assert.Same(action.FilterDescriptors[1].Filter, f)); // Copied by provider

            // Act - 2
            var filters2 = cache.GetFilters(context);

            Assert.Same(filters1, filters2);

            Assert.Collection(
                filters2,
                f => Assert.Same(filters1[0], f), // Cached
                f => Assert.Same(filters1[1], f)); // Cached
        }

        [Fact]
        public void GetFilters_DoesNotCacheFiltersWithIsReusableFalse()
        {
            // Arrange
            var services = CreateServices();
            var cache = CreateCache(new DefaultFilterProvider());

            var action = new ControllerActionDescriptor()
            {
                FilterDescriptors = new[]
                {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                    new FilterDescriptor(new TestFilter(), FilterScope.Action),
                },
            };

            var context = new ActionContext(new DefaultHttpContext(), new RouteData(), action);

            // Act - 1
            var filters1 = cache.GetFilters(context);

            // Assert - 1
            Assert.Collection(
                filters1,
                f => Assert.NotSame(action.FilterDescriptors[0].Filter, f), // Created by factory
                f => Assert.Same(action.FilterDescriptors[1].Filter, f)); // Copied by provider

            // Act - 2
            var filters2 = cache.GetFilters(context);

            Assert.NotSame(filters1, filters2);

            Assert.Collection(
                filters2,
                f => Assert.NotSame(filters1[0], f), // Created by factory (again)
                f => Assert.Same(filters1[1], f)); // Cached
        }

        private class TestFilter : IFilterMetadata
        {
        }

        private class TestFilterFactory : IFilterFactory
        {
            public bool IsReusable { get; set; }

            public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
            {
                return new TestFilter();
            }
        }

        private static IServiceProvider CreateServices()
        {
            return new ServiceCollection().BuildServiceProvider();
        }

        private static FilterCache CreateCache(params IFilterProvider[] providers)
        {
            var services = CreateServices();
            var descriptorProvider = new DefaultActionDescriptorCollectionProvider(services);
            return new FilterCache(descriptorProvider, providers);
        }
    }
}
