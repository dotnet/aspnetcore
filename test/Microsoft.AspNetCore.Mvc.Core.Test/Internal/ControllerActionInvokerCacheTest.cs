// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerCacheTest
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
                MethodInfo = typeof(ControllerActionInvokerCache).GetMethod(
                    nameof(ControllerActionInvokerCache.GetControllerActionMethodExecutor)),
                ControllerTypeInfo = typeof(ControllerActionInvokerCache).GetTypeInfo()
            };

            var context = new ControllerContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                action));

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
                MethodInfo = typeof(ControllerActionInvokerCache).GetMethod(
                    nameof(ControllerActionInvokerCache.GetControllerActionMethodExecutor)),
                ControllerTypeInfo = typeof(ControllerActionInvokerCache).GetTypeInfo()
            };

            var context = new ControllerContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                action));

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
                MethodInfo = typeof(ControllerActionInvokerCache).GetMethod(
                    nameof(ControllerActionInvokerCache.GetControllerActionMethodExecutor)),
                ControllerTypeInfo = typeof(ControllerActionInvokerCache).GetTypeInfo()
            };

            var context = new ControllerContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                action));

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

        [Fact]
        public void GetControllerActionMethodExecutor_CachesExecutor()
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
                MethodInfo = typeof(ControllerActionInvokerCache).GetMethod(
                    nameof(ControllerActionInvokerCache.GetControllerActionMethodExecutor)),
                ControllerTypeInfo = typeof(ControllerActionInvokerCache).GetTypeInfo()

            };

            var context = new ControllerContext(
                new ActionContext(new DefaultHttpContext(),
                new RouteData(),
                action));

            // Act - 1            
            var executor1 = cache.GetControllerActionMethodExecutor(context);

            Assert.NotNull(executor1);

            var filters1 = cache.GetFilters(context);

            Assert.NotNull(filters1);

            // Act - 2
            var executor2 = cache.GetControllerActionMethodExecutor(context);

            Assert.Same(executor1, executor2);
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

        private static ControllerActionInvokerCache CreateCache(params IFilterProvider[] providers)
        {
            var services = CreateServices();
            var descriptorProvider = new ActionDescriptorCollectionProvider(services);
            return new ControllerActionInvokerCache(descriptorProvider, providers);
        }
    }
}
