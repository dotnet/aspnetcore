// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageFilterFactoryProviderTest
    {
        [Fact]
        public void FilterFactory_ReturnsNoFilters_IfNoFiltersAreSpecified()
        {
            // Arrange
            var filterProviders = new IFilterProvider[0];
            var actionInvokerProviderContext = GetInvokerContext();

            // Act
            var filterFactory = PageFilterFactoryProvider.GetFilterFactory(
                filterProviders,
                actionInvokerProviderContext);
            var filters1 = filterFactory(actionInvokerProviderContext.ActionContext);
            var filters2 = filterFactory(actionInvokerProviderContext.ActionContext);

            // Assert
            Assert.Empty(filters1);
            Assert.Empty(filters2);
        }

        [Fact]
        public void FilterFactory_ReturnsNoFilters_IfAllFiltersAreRemoved()
        {
            // Arrange
            var filterProvider = new TestFilterProvider(
                context => context.Results.Clear());
            var filter = new FilterDescriptor(new TypeFilterAttribute(typeof(object)), FilterScope.Global);
            var actionInvokerProviderContext = GetInvokerContext(filter);

            // Act
            var filterFactory = PageFilterFactoryProvider.GetFilterFactory(
                new[] { filterProvider },
                actionInvokerProviderContext);
            var filters1 = filterFactory(actionInvokerProviderContext.ActionContext);
            var filters2 = filterFactory(actionInvokerProviderContext.ActionContext);

            // Assert
            Assert.Empty(filters1);
            Assert.Empty(filters2);
        }

        [Fact]
        public void FilterFactory_CachesAllFilters()
        {
            // Arrange
            var staticFilter1 = new TestFilter();
            var staticFilter2 = new TestFilter();
            var actionInvokerProviderContext = GetInvokerContext(new[]
            {
                new FilterDescriptor(staticFilter1, FilterScope.Action),
                new FilterDescriptor(staticFilter2, FilterScope.Action),
            });
            var filterProviders = new[] { new DefaultFilterProvider() };

            // Act - 1
            var filterFactory = PageFilterFactoryProvider.GetFilterFactory(
                filterProviders,
                actionInvokerProviderContext);

            var request1Filters = filterFactory(actionInvokerProviderContext.ActionContext);

            // Assert - 1
            Assert.Collection(
                request1Filters,
                f => Assert.Same(staticFilter1, f),
                f => Assert.Same(staticFilter2, f));

            // Act - 2
            var request2Filters = filterFactory(actionInvokerProviderContext.ActionContext);

            // Assert - 2
            Assert.Collection(
                request2Filters,
                f => Assert.Same(staticFilter1, f),
                f => Assert.Same(staticFilter2, f));
        }

        [Fact]
        public void FilterFactory_CachesFilterFromFactory()
        {
            // Arrange
            var staticFilter = new TestFilter();
            var actionInvokerProviderContext = GetInvokerContext(new[]
            {
                new FilterDescriptor(new TestFilterFactory() { IsReusable = true }, FilterScope.Action),
                new FilterDescriptor(staticFilter, FilterScope.Action),
            });
            var filterProviders = new[] { new DefaultFilterProvider() };

            // Act & Assert
            var filterFactory = PageFilterFactoryProvider.GetFilterFactory(
                filterProviders,
                actionInvokerProviderContext);

            var filters = filterFactory(actionInvokerProviderContext.ActionContext);
            Assert.Equal(2, filters.Length);
            var cachedFactoryCreatedFilter = Assert.IsType<TestFilter>(filters[0]); // Created by factory
            Assert.Same(staticFilter, filters[1]); // Cached and the same statically created filter instance

            for (var i = 0; i < 5; i++)
            {
                filters = filterFactory(actionInvokerProviderContext.ActionContext);

                var currentFactoryCreatedFilter = filters[0];
                Assert.Same(currentFactoryCreatedFilter, cachedFactoryCreatedFilter); // Cached
                Assert.Same(staticFilter, filters[1]); // Cached
            }
        }

        [Fact]
        public void FilterFactory_DoesNotCacheFiltersWithIsReusableFalse()
        {
            // Arrange
            var staticFilter = new TestFilter();
            var actionInvokerProviderContext = GetInvokerContext(new[]
            {
                new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                new FilterDescriptor(staticFilter, FilterScope.Action),
            });
            var filterProviders = new[] { new DefaultFilterProvider() };

            // Act & Assert
            var filterFactory = PageFilterFactoryProvider.GetFilterFactory(
                filterProviders,
                actionInvokerProviderContext);
            IFilterMetadata previousFactoryCreatedFilter = null;
            for (var i = 0; i < 5; i++)
            {
                var filters = filterFactory(actionInvokerProviderContext.ActionContext);

                var currentFactoryCreatedFilter = filters[0];
                Assert.NotSame(currentFactoryCreatedFilter, previousFactoryCreatedFilter); // Never Cached
                Assert.Same(staticFilter, filters[1]); // Cached

                previousFactoryCreatedFilter = currentFactoryCreatedFilter;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FilterFactory_FiltersAddedByFilterProviders_AreNeverCached(bool reusable)
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
                });
            var staticFilter = new TestFilter();
            var actionInvokerProviderContext = GetInvokerContext(new[]
            {
                new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                new FilterDescriptor(staticFilter, FilterScope.Action),
            });
            var actionContext = actionInvokerProviderContext.ActionContext;
            var filterProviders = new IFilterProvider[] { new DefaultFilterProvider(), customFilterProvider };


            // Act - 1
            actionContext.HttpContext.Items["name"] = "foo";
            var filterFactory = PageFilterFactoryProvider.GetFilterFactory(
                filterProviders,
                actionInvokerProviderContext);
            var filters = filterFactory(actionContext);

            // Assert - 1
            Assert.Equal(3, filters.Length);
            var request1Filter1 = Assert.IsType<TestFilter>(filters[0]); // Created by factory
            Assert.Same(staticFilter, filters[1]); // Cached and the same statically created filter instance
            var request1Filter3 = Assert.IsType<TestFilter>(filters[2]); // Created by custom filter provider
            Assert.Equal("foo", request1Filter3.Data);

            // Act - 2
            actionContext.HttpContext.Items["name"] = "bar";
            filters = filterFactory(actionContext);

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
            private TestFilter _testFilter;

            public bool IsReusable { get; set; }

            public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
            {
                if (IsReusable)
                {
                    if (_testFilter == null)
                    {
                        _testFilter = new TestFilter();
                    }
                    return _testFilter;
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
                Action<FilterProviderContext> providerExecuted = null,
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

        private static ActionInvokerProviderContext GetInvokerContext(params FilterDescriptor[] filters)
        {
            var actionDescriptor = new PageActionDescriptor
            {
                FilterDescriptors = new List<FilterDescriptor>(filters ?? Enumerable.Empty<FilterDescriptor>())
            };
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
            return new ActionInvokerProviderContext(actionContext);
        }
    }
}
