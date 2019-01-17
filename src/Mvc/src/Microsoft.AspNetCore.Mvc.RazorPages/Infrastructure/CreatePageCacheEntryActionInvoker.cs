// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Executing a Razor Page requires ensuring it's compiled which is an asynchronous operation. 
    /// The <see cref="IActionInvokerProvider"/> pipeline is synchronous and consequently we cannot compile a Razor Page
    /// as part of executing it without blocking. The <see cref="PageActionInvokerProvider"/> instead delegates to this type
    /// to compile the Razor Page and cache the information required for page invocation.
    /// </summary>
    internal class CreatePageCacheEntryActionInvoker : IActionInvoker
    {
        private readonly PageLoaderBase _loader;
        private readonly IPageFactoryProvider _pageFactoryProvider;
        private readonly IPageModelFactoryProvider _modelFactoryProvider;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IRazorPageFactoryProvider _razorPageFactoryProvider;
        private readonly ParameterBinder _parameterBinder;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IPageActionInvokerFactory _pageActionInvokerFactory;
        private readonly ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry> _cache;

        private readonly PageActionDescriptor _actionDescriptor;
        private readonly ActionContext _actionContext;

        public CreatePageCacheEntryActionInvoker(
            PageLoaderBase loader,
            IPageFactoryProvider pageFactoryProvider,
            IPageModelFactoryProvider modelFactoryProvider,
            IRazorPageFactoryProvider razorPageFactoryProvider,
            IFilterProvider[] filterProviders,
            ParameterBinder parameterBinder,
            IModelBinderFactory modelBinderFactory,
            IModelMetadataProvider modelMetadataProvider,
            IPageActionInvokerFactory pageActionInvokerFactory,
            ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry> cache,
            ActionContext actionContext,
            PageActionDescriptor actionDescriptor)
        {
            _loader = loader;
            _pageFactoryProvider = pageFactoryProvider;
            _modelFactoryProvider = modelFactoryProvider;
            _razorPageFactoryProvider = razorPageFactoryProvider;

            _filterProviders = filterProviders;
            _parameterBinder = parameterBinder;
            _modelBinderFactory = modelBinderFactory;
            _modelMetadataProvider = modelMetadataProvider;
            _pageActionInvokerFactory = pageActionInvokerFactory;

            _cache = cache;

            _actionContext = actionContext;
            _actionDescriptor = actionDescriptor;
        }

        public async Task InvokeAsync()
        {
            var compiledPageActionDescriptor = await _loader.LoadAsync(_actionDescriptor);
            _actionContext.ActionDescriptor = compiledPageActionDescriptor;

            var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, _actionContext);
            var filters = filterFactoryResult.Filters;
            var cacheEntry = CreateCacheEntry(compiledPageActionDescriptor, filterFactoryResult.CacheableFilters);
            cacheEntry = _cache.GetOrAdd(compiledPageActionDescriptor, cacheEntry);

            var invoker = _pageActionInvokerFactory.CreateInvoker(_actionContext, cacheEntry, filters);
            await invoker.InvokeAsync();
        }

        private PageActionInvokerCacheEntry CreateCacheEntry(CompiledPageActionDescriptor compiledPageActionDescriptor, FilterItem[] cachedFilters)
        {
            var viewDataFactory = ViewDataDictionaryFactory.CreateFactory(compiledPageActionDescriptor.DeclaredModelTypeInfo);

            var pageFactory = _pageFactoryProvider.CreatePageFactory(compiledPageActionDescriptor);
            var pageDisposer = _pageFactoryProvider.CreatePageDisposer(compiledPageActionDescriptor);
            var propertyBinder = PageBinderFactory.CreatePropertyBinder(
                _parameterBinder,
                _modelMetadataProvider,
                _modelBinderFactory,
                compiledPageActionDescriptor);

            Func<PageContext, object> modelFactory = null;
            Action<PageContext, object> modelReleaser = null;
            if (compiledPageActionDescriptor.ModelTypeInfo != compiledPageActionDescriptor.PageTypeInfo)
            {
                modelFactory = _modelFactoryProvider.CreateModelFactory(compiledPageActionDescriptor);
                modelReleaser = _modelFactoryProvider.CreateModelDisposer(compiledPageActionDescriptor);
            }

            var viewStartFactories = GetViewStartFactories(compiledPageActionDescriptor);

            var handlerExecutors = GetHandlerExecutors(compiledPageActionDescriptor);
            var handlerBinders = GetHandlerBinders(compiledPageActionDescriptor);

            return new PageActionInvokerCacheEntry(
                compiledPageActionDescriptor,
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
}
