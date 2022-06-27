// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class PageActionInvokerCache
{
    private readonly IPageFactoryProvider _pageFactoryProvider;
    private readonly IPageModelFactoryProvider _modelFactoryProvider;
    private readonly IModelBinderFactory _modelBinderFactory;
    private readonly IRazorPageFactoryProvider _razorPageFactoryProvider;
    private readonly IFilterProvider[] _filterProviders;
    private readonly ParameterBinder _parameterBinder;
    private readonly IModelMetadataProvider _modelMetadataProvider;

    public PageActionInvokerCache(
        IPageFactoryProvider pageFactoryProvider,
        IPageModelFactoryProvider modelFactoryProvider,
        IRazorPageFactoryProvider razorPageFactoryProvider,
        IEnumerable<IFilterProvider> filterProviders,
        ParameterBinder parameterBinder,
        IModelMetadataProvider modelMetadataProvider,
        IModelBinderFactory modelBinderFactory)
    {
        _pageFactoryProvider = pageFactoryProvider;
        _modelFactoryProvider = modelFactoryProvider;
        _modelBinderFactory = modelBinderFactory;
        _razorPageFactoryProvider = razorPageFactoryProvider;
        _filterProviders = filterProviders.ToArray();
        _parameterBinder = parameterBinder;
        _modelMetadataProvider = modelMetadataProvider;
    }

    public (PageActionInvokerCacheEntry cacheEntry, IFilterMetadata[] filters) GetCachedResult(ActionContext actionContext)
    {
        var actionDescriptor = (PageActionDescriptor)actionContext.ActionDescriptor;

        var compiledPageActionDescriptor = actionDescriptor.CompiledPageDescriptor;

        Debug.Assert(compiledPageActionDescriptor != null, "PageLoader didn't run!");

        var cacheEntry = compiledPageActionDescriptor.CacheEntry;

        IFilterMetadata[] filters;
        if (cacheEntry is null)
        {
            actionContext.ActionDescriptor = compiledPageActionDescriptor;
            var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, actionContext);
            filters = filterFactoryResult.Filters;
            cacheEntry = CreateCacheEntry(compiledPageActionDescriptor, filterFactoryResult.CacheableFilters);
            compiledPageActionDescriptor.CacheEntry = cacheEntry;
        }
        else
        {
            filters = FilterFactory.CreateUncachedFilters(
                _filterProviders,
                actionContext,
                cacheEntry.CacheableFilters);
        }

        return (cacheEntry, filters);
    }

    private PageActionInvokerCacheEntry CreateCacheEntry(
        CompiledPageActionDescriptor compiledActionDescriptor,
        FilterItem[] cachedFilters)
    {
        var viewDataFactory = ViewDataDictionaryFactory.CreateFactory(compiledActionDescriptor.DeclaredModelTypeInfo);

        var pageFactory = _pageFactoryProvider.CreatePageFactory(compiledActionDescriptor);
        var pageDisposer = _pageFactoryProvider.CreateAsyncPageDisposer(compiledActionDescriptor);
        var propertyBinder = PageBinderFactory.CreatePropertyBinder(
            _parameterBinder,
            _modelMetadataProvider,
            _modelBinderFactory,
            compiledActionDescriptor);

        Func<PageContext, object>? modelFactory = null;
        Func<PageContext, object, ValueTask>? modelReleaser = null;
        if (compiledActionDescriptor.ModelTypeInfo != compiledActionDescriptor.PageTypeInfo)
        {
            modelFactory = _modelFactoryProvider.CreateModelFactory(compiledActionDescriptor);
            modelReleaser = _modelFactoryProvider.CreateAsyncModelDisposer(compiledActionDescriptor);
        }

        var viewStartFactories = GetViewStartFactories(compiledActionDescriptor);

        var handlerExecutors = GetHandlerExecutors(compiledActionDescriptor);
        var handlerBinders = GetHandlerBinders(compiledActionDescriptor);

        return new PageActionInvokerCacheEntry(
            compiledActionDescriptor,
            viewDataFactory,
            pageFactory,
            pageDisposer,
            modelFactory,
            modelReleaser,
            propertyBinder,
            handlerExecutors,
            handlerBinders,
            viewStartFactories,
            cachedFilters);
    }

    // Internal for testing.
    internal List<Func<IRazorPage>> GetViewStartFactories(CompiledPageActionDescriptor descriptor)
    {
        var viewStartFactories = new List<Func<IRazorPage>>();
        // Always pick up all _ViewStarts, including the ones outside the Pages root.
        foreach (var filePath in RazorFileHierarchy.GetViewStartPaths(descriptor.RelativePath))
        {
            var factoryResult = _razorPageFactoryProvider.CreateFactory(filePath);
            if (factoryResult.Success)
            {
                viewStartFactories.Insert(0, factoryResult.RazorPageFactory);
            }
        }

        return viewStartFactories;
    }

    private static PageHandlerExecutorDelegate[] GetHandlerExecutors(CompiledPageActionDescriptor actionDescriptor)
    {
        if (actionDescriptor.HandlerMethods == null || actionDescriptor.HandlerMethods.Count == 0)
        {
            return Array.Empty<PageHandlerExecutorDelegate>();
        }

        var results = new PageHandlerExecutorDelegate[actionDescriptor.HandlerMethods.Count];

        for (var i = 0; i < actionDescriptor.HandlerMethods.Count; i++)
        {
            results[i] = ExecutorFactory.CreateExecutor(actionDescriptor.HandlerMethods[i]);
        }

        return results;
    }

    private PageHandlerBinderDelegate[] GetHandlerBinders(CompiledPageActionDescriptor actionDescriptor)
    {
        if (actionDescriptor.HandlerMethods == null || actionDescriptor.HandlerMethods.Count == 0)
        {
            return Array.Empty<PageHandlerBinderDelegate>();
        }

        var results = new PageHandlerBinderDelegate[actionDescriptor.HandlerMethods.Count];

        for (var i = 0; i < actionDescriptor.HandlerMethods.Count; i++)
        {
            results[i] = PageBinderFactory.CreateHandlerBinder(
                _parameterBinder,
                _modelMetadataProvider,
                _modelBinderFactory,
                actionDescriptor,
                actionDescriptor.HandlerMethods[i]);
        }

        return results;
    }
}
