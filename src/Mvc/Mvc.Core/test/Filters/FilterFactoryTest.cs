// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class FilterFactoryTest
{
    [Fact]
    public void GetAllFilters_ReturnsNoFilters_IfNoFiltersAreSpecified()
    {
        // Arrange
        var filterProviders = new IFilterProvider[0];
        var actionContext = CreateActionContext(new FilterDescriptor[0]);

        // Act
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);

        // Assert
        Assert.Empty(filterResult.CacheableFilters);
        Assert.Empty(filterResult.Filters);
    }

    [Fact]
    public void GetAllFilters_ReturnsNoFilters_IfAllFiltersAreRemoved()
    {
        // Arrange
        var filterProvider = new TestFilterProvider(
            context => context.Results.Clear(),
            content => { });
        var filter = new FilterDescriptor(new TypeFilterAttribute(typeof(object)), FilterScope.Global);
        var actionContext = CreateActionContext(new[] { filter });

        // Act
        var filterResult = FilterFactory.GetAllFilters(
            new[] { filterProvider },
            actionContext);

        // Assert
        Assert.Collection(filterResult.CacheableFilters,
            f =>
            {
                Assert.Null(f.Filter);
                Assert.False(f.IsReusable);
            });
        Assert.Empty(filterResult.Filters);
    }

    [Fact]
    public void GetAllFilters_CachesAllFilters()
    {
        // Arrange
        var staticFilter1 = new TestFilter();
        var staticFilter2 = new TestFilter();
        var actionContext = CreateActionContext(new[]
            {
                    new FilterDescriptor(staticFilter1, FilterScope.Action),
                    new FilterDescriptor(staticFilter2, FilterScope.Action),
                });
        var filterProviders = new[] { new DefaultFilterProvider() };

        // Act - 1
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);
        var request1Filters = filterResult.Filters;

        // Assert - 1
        Assert.Collection(
            request1Filters,
            f => Assert.Same(staticFilter1, f),
            f => Assert.Same(staticFilter2, f));

        // Act - 2
        var request2Filters = FilterFactory.CreateUncachedFilters(
            filterProviders,
            actionContext,
            filterResult.CacheableFilters);

        // Assert - 2
        Assert.Collection(
            request2Filters,
            f => Assert.Same(staticFilter1, f),
            f => Assert.Same(staticFilter2, f));
    }

    [Fact]
    public void GetAllFilters_OrdersFilters()
    {
        // Arrange
        var filter1 = new TestOrderedFilter { Order = 1000 };
        var filter2 = new TestFilter();
        var filter3 = new TestOrderedFilter { Order = 10 };
        var actionContext = CreateActionContext(new[]
        {
                new FilterDescriptor(filter1, FilterScope.Action),
                new FilterDescriptor(filter2, FilterScope.Action),
                new FilterDescriptor(filter3, FilterScope.Action),
            });
        var filterProviders = new[] { new DefaultFilterProvider() };

        // Act
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);

        // Assert
        Assert.Collection(
            filterResult.Filters,
            f => Assert.Same(filter2, f),
            f => Assert.Same(filter3, f),
            f => Assert.Same(filter1, f));
    }

    [Fact]
    public void GetAllFilters_CachesFilterOrder()
    {
        // Arrange
        var filter1 = new TestOrderedFilter { Order = 1000 };
        var filter2 = new TestFilter();
        var filter3 = new TestOrderedFilter { Order = 10 };
        var actionContext = CreateActionContext(new[]
        {
                new FilterDescriptor(filter1, FilterScope.Action),
                new FilterDescriptor(filter2, FilterScope.Action),
                new FilterDescriptor(filter3, FilterScope.Action),
            });
        var filterProviders = new[] { new DefaultFilterProvider() };

        // Act
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);
        var requestFilters = FilterFactory.CreateUncachedFilters(
            filterProviders,
            actionContext,
            filterResult.CacheableFilters);

        // Assert
        Assert.Collection(
            requestFilters,
            f => Assert.Same(filter2, f),
            f => Assert.Same(filter3, f),
            f => Assert.Same(filter1, f));
    }

    [Fact]
    public void GetAllFilters_CachesFilterFromFactory()
    {
        // Arrange
        var staticFilter = new TestFilter();
        var actionContext = CreateActionContext(new[]
            {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = true }, FilterScope.Action),
                    new FilterDescriptor(staticFilter, FilterScope.Action),
                });
        var filterProviders = new[] { new DefaultFilterProvider() };
        var filterDescriptors = actionContext.ActionDescriptor.FilterDescriptors;

        // Act & Assert
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);

        var filters = filterResult.Filters;
        Assert.Equal(2, filters.Length);
        var cachedFactoryCreatedFilter = Assert.IsType<TestFilter>(filters[0]); // Created by factory
        Assert.Same(staticFilter, filters[1]); // Cached and the same statically created filter instance

        for (var i = 0; i < 5; i++)
        {
            filters = FilterFactory.CreateUncachedFilters(filterProviders, actionContext, filterResult.CacheableFilters);

            var currentFactoryCreatedFilter = filters[0];
            Assert.Same(currentFactoryCreatedFilter, cachedFactoryCreatedFilter); // Cached
            Assert.Same(staticFilter, filters[1]); // Cached
        }
    }

    [Fact]
    public void GetAllFilters_DoesNotCacheFiltersWithIsReusableFalse()
    {
        // Arrange
        var staticFilter = new TestFilter();
        var actionContext = CreateActionContext(new[]
            {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                    new FilterDescriptor(staticFilter, FilterScope.Action),
                });
        var filterProviders = new[] { new DefaultFilterProvider() };
        var filterDescriptors = actionContext.ActionDescriptor.FilterDescriptors;

        // Act & Assert
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);
        var filters = filterResult.Filters;
        IFilterMetadata previousFactoryCreatedFilter = null;
        for (var i = 0; i < 5; i++)
        {
            filters = FilterFactory.CreateUncachedFilters(filterProviders, actionContext, filterResult.CacheableFilters);

            var currentFactoryCreatedFilter = filters[0];
            Assert.NotSame(currentFactoryCreatedFilter, previousFactoryCreatedFilter); // Never Cached
            Assert.Same(staticFilter, filters[1]); // Cached

            previousFactoryCreatedFilter = currentFactoryCreatedFilter;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetAllFilters_FiltersAddedByFilterProviders_AreNeverCached(bool reusable)
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
        var actionContext = CreateActionContext(new[]
            {
                    new FilterDescriptor(new TestFilterFactory() { IsReusable = false }, FilterScope.Action),
                    new FilterDescriptor(staticFilter, FilterScope.Action),
                });
        var filterProviders = new IFilterProvider[] { new DefaultFilterProvider(), customFilterProvider };
        var filterDescriptors = actionContext.ActionDescriptor.FilterDescriptors;

        // Act - 1
        actionContext.HttpContext.Items["name"] = "foo";
        var filterResult = FilterFactory.GetAllFilters(filterProviders, actionContext);
        var filters = filterResult.Filters;

        // Assert - 1
        Assert.Equal(3, filters.Length);
        var request1Filter1 = Assert.IsType<TestFilter>(filters[0]); // Created by factory
        Assert.Same(staticFilter, filters[1]); // Cached and the same statically created filter instance
        var request1Filter3 = Assert.IsType<TestFilter>(filters[2]); // Created by custom filter provider
        Assert.Equal("foo", request1Filter3.Data);

        // Act - 2
        actionContext.HttpContext.Items["name"] = "bar";
        filters = FilterFactory.CreateUncachedFilters(filterProviders, actionContext, filterResult.CacheableFilters);

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

    private class TestOrderedFilter : IFilterMetadata, IOrderedFilter
    {
        public int Order { get; set; }
    }

    private static ActionContext CreateActionContext(FilterDescriptor[] filterDescriptors)
    {
        var actionDescriptor = new ActionDescriptor
        {
            FilterDescriptors = filterDescriptors,
        };

        return new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
    }
}
