// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerCacheTest
    {
        [Fact]
        public void GetFilters_CachesAllFilters()
        {
            // Arrange
            var staticFilter1 = new TestFilter();
            var staticFilter2 = new TestFilter();
            var controllerContext = CreateControllerContext(new[]
                {
                    new FilterDescriptor(staticFilter1, FilterScope.Action),
                    new FilterDescriptor(staticFilter2, FilterScope.Action),
                });
            var controllerActionInvokerCache = CreateControllerActionInvokerCache(
                controllerContext,
                new[] { new DefaultFilterProvider() });

            // Act - 1
            var request1Filters = controllerActionInvokerCache.GetState(controllerContext).Filters;

            // Assert - 1
            Assert.Collection(
                request1Filters,
                f => Assert.Same(staticFilter1, f),
                f => Assert.Same(staticFilter2, f));

            // Act - 2
            var request2Filters = controllerActionInvokerCache.GetState(controllerContext).Filters;

            // Assert - 2
            Assert.Collection(
                request2Filters,
                f => Assert.Same(staticFilter1, f),
                f => Assert.Same(staticFilter2, f));
        }

        [Fact]
        public void GetFilters_CachesFilterFromFactory()
        {
            // Arrange
            var staticFilter = new TestFilter();
            var controllerContext = CreateControllerContext(new[]
                {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = true }, FilterScope.Action),
                    new FilterDescriptor(staticFilter, FilterScope.Action),
                });
            var controllerActionInvokerCache = CreateControllerActionInvokerCache(
                controllerContext,
                new[] { new DefaultFilterProvider() });
            var filterDescriptors = controllerContext.ActionDescriptor.FilterDescriptors;

            // Act & Assert
            var filters = controllerActionInvokerCache.GetState(controllerContext).Filters;
            Assert.Equal(2, filters.Length);
            var cachedFactoryCreatedFilter = Assert.IsType<TestFilter>(filters[0]); // Created by factory
            Assert.Same(staticFilter, filters[1]); // Cached and the same statically created filter instance

            for (var i = 0; i < 5; i++)
            {
                filters = controllerActionInvokerCache.GetState(controllerContext).Filters;

                var currentFactoryCreatedFilter = filters[0];
                Assert.Same(currentFactoryCreatedFilter, cachedFactoryCreatedFilter); // Cached
                Assert.Same(staticFilter, filters[1]); // Cached
            }
        }

        [Fact]
        public void GetFilters_DoesNotCacheFiltersWithIsReusableFalse()
        {
            // Arrange
            var staticFilter = new TestFilter();
            var controllerContext = CreateControllerContext(new[]
                {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                    new FilterDescriptor(staticFilter, FilterScope.Action),
                });
            var controllerActionInvokerCache = CreateControllerActionInvokerCache(
                controllerContext,
                new[] { new DefaultFilterProvider() });
            var filterDescriptors = controllerContext.ActionDescriptor.FilterDescriptors;

            // Act & Assert
            IFilterMetadata previousFactoryCreatedFilter = null;
            for (var i = 0; i < 5; i++)
            {
                var filters = controllerActionInvokerCache.GetState(controllerContext).Filters;

                var currentFactoryCreatedFilter = filters[0];
                Assert.NotSame(currentFactoryCreatedFilter, previousFactoryCreatedFilter); // Never Cached
                Assert.Same(staticFilter, filters[1]); // Cached

                previousFactoryCreatedFilter = currentFactoryCreatedFilter;
            }
        }

        [Fact]
        public void GetControllerActionMethodExecutor_CachesActionMethodExecutor()
        {
            // Arrange
            var filter = new TestFilter();
            var controllerContext = CreateControllerContext(new[]
                {
                    new FilterDescriptor(filter, FilterScope.Action)
                });
            var controllerActionInvokerCache = CreateControllerActionInvokerCache(
                controllerContext,
                new[] { new DefaultFilterProvider() });

            // Act
            var cacheEntry1 = controllerActionInvokerCache.GetState(controllerContext);
            var cacheEntry2 = controllerActionInvokerCache.GetState(controllerContext);

            // Assert
            Assert.Same(cacheEntry1.ActionMethodExecutor, cacheEntry2.ActionMethodExecutor);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetFilters_FiltersAddedByFilterProviders_AreNeverCached(bool reusable)
        {
            // Arrange
            var customFilterProvider = new TestFilterProvider(
                    providerExecuting: (providerContext) =>
                    {
                        var filter = new TestFilter(providerContext.ActionContext.HttpContext.Items["name"] as string);
                        providerContext.Results.Add(
                            new FilterItem(new FilterDescriptor(filter, FilterScope.Global), filter)
                            {
                                IsReusable = reusable
                            });
                    },
                    providerExecuted: null);
            var staticFilter = new TestFilter();
            var controllerContext = CreateControllerContext(new[]
                {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                    new FilterDescriptor(staticFilter, FilterScope.Action),
                });
            var controllerActionInvokerCache = CreateControllerActionInvokerCache(
                controllerContext,
                new IFilterProvider[] { new DefaultFilterProvider(), customFilterProvider });
            var filterDescriptors = controllerContext.ActionDescriptor.FilterDescriptors;

            // Act - 1
            controllerContext.HttpContext.Items["name"] = "foo";
            var filters = controllerActionInvokerCache.GetState(controllerContext).Filters;

            // Assert - 1
            Assert.Equal(3, filters.Length);
            var request1Filter1 = Assert.IsType<TestFilter>(filters[0]); // Created by factory
            Assert.Same(staticFilter, filters[1]); // Cached and the same statically created filter instance
            var request1Filter3 = Assert.IsType<TestFilter>(filters[2]); // Created by custom filter provider
            Assert.Equal("foo", request1Filter3.Data);

            // Act - 2
            controllerContext.HttpContext.Items["name"] = "bar";
            filters = controllerActionInvokerCache.GetState(controllerContext).Filters;

            // Assert -2
            Assert.Equal(3, filters.Length);
            var request2Filter1 = Assert.IsType<TestFilter>(filters[0]);
            Assert.NotSame(request1Filter1, request2Filter1); // Created by factory
            Assert.Same(staticFilter, filters[1]);   // Cached and the same statically created filter instance
            var request2Filter3 = Assert.IsType<TestFilter>(filters[2]);
            Assert.NotSame(request1Filter3, request2Filter3); // Created by custom filter provider again
            Assert.Equal("bar", request2Filter3.Data);
        }

        private class TestFilter : IFilterMetadata
        {
            public TestFilter()
            {
            }

            public TestFilter(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        private class TestFilterFactory : IFilterFactory
        {
            private TestFilter testFilter;

            public bool IsReusable { get; set; }

            public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
            {
                if (IsReusable)
                {
                    if (testFilter == null)
                    {
                        testFilter = new TestFilter();
                    }
                    return testFilter;
                }
                else
                {
                    return new TestFilter();
                }
            }
        }

        private class TestFilterProvider : IFilterProvider
        {
            private readonly Action<FilterProviderContext> _providerExecuting;
            private readonly Action<FilterProviderContext> _providerExecuted;

            public TestFilterProvider(
                Action<FilterProviderContext> providerExecuting,
                Action<FilterProviderContext> providerExecuted,
                int order = 0)
            {
                _providerExecuting = providerExecuting;
                _providerExecuted = providerExecuted;
                Order = order;
            }

            public int Order { get; }

            public void OnProvidersExecuting(FilterProviderContext context)
            {
                _providerExecuting?.Invoke(context);
            }

            public void OnProvidersExecuted(FilterProviderContext context)
            {
                _providerExecuted?.Invoke(context);
            }
        }

        private class TestController
        {
            public void Index()
            {
            }
        }

        private class CustomActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
        {
            public CustomActionDescriptorCollectionProvider(ControllerActionDescriptor[] actionDescriptors)
            {
                ActionDescriptors = new ActionDescriptorCollection(actionDescriptors, version: 1);
            }

            public ActionDescriptorCollection ActionDescriptors { get; }
        }

        private static ControllerActionInvokerCache CreateControllerActionInvokerCache(
            ControllerContext controllerContext,
            IFilterProvider[] filterProviders)
        {
            var descriptorProvider = new CustomActionDescriptorCollectionProvider(
                new[] { controllerContext.ActionDescriptor });
            return new ControllerActionInvokerCache(descriptorProvider, filterProviders);
        }

        private static ControllerContext CreateControllerContext(FilterDescriptor[] filterDescriptors)
        {
            var actionDescriptor = new ControllerActionDescriptor()
            {
                FilterDescriptors = filterDescriptors,
                MethodInfo = typeof(TestController).GetMethod(nameof(TestController.Index)),
                ControllerTypeInfo = typeof(TestController).GetTypeInfo()
            };

            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
            return new ControllerContext(actionContext);
        }
    }
}
