// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerProvider : IActionInvokerProvider
    {
        private const string ModelPropertyName = "Model";
        private readonly IPageLoader _loader;
        private readonly IPageFactoryProvider _pageFactoryProvider;
        private readonly IPageModelFactoryProvider _modelFactoryProvider;
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IFilterProvider[] _filterProviders;
        private readonly IReadOnlyList<IValueProviderFactory> _valueProviderFactories;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly IPageHandlerMethodSelector _selector;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger<PageActionInvoker> _logger;
        private volatile InnerCache _currentCache;

        public PageActionInvokerProvider(
            IPageLoader loader,
            IPageFactoryProvider pageFactoryProvider,
            IPageModelFactoryProvider modelFactoryProvider,
            IActionDescriptorCollectionProvider collectionProvider,
            IEnumerable<IFilterProvider> filterProviders,
            IEnumerable<IValueProviderFactory> valueProviderFactories,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataFactory,
            IOptions<HtmlHelperOptions> htmlHelperOptions,
            IPageHandlerMethodSelector selector,
            DiagnosticSource diagnosticSource,
            ILoggerFactory loggerFactory)
        {
            _loader = loader;
            _pageFactoryProvider = pageFactoryProvider;
            _modelFactoryProvider = modelFactoryProvider;
            _collectionProvider = collectionProvider;
            _filterProviders = filterProviders.ToArray();
            _valueProviderFactories = valueProviderFactories.ToArray();
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataFactory = tempDataFactory;
            _htmlHelperOptions = htmlHelperOptions.Value;
            _selector = selector;
            _diagnosticSource = diagnosticSource;
            _logger = loggerFactory.CreateLogger<PageActionInvoker>();
        }

        public int Order { get; } = -1000;

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var actionContext = context.ActionContext;
            var actionDescriptor = actionContext.ActionDescriptor as PageActionDescriptor;
            if (actionDescriptor == null)
            {
                return;
            }

            var cache = CurrentCache;
            PageActionInvokerCacheEntry cacheEntry;

            IFilterMetadata[] filters;
            if (!cache.Entries.TryGetValue(actionDescriptor, out cacheEntry))
            {
                var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, actionContext);
                filters = filterFactoryResult.Filters;
                cacheEntry = CreateCacheEntry(context, filterFactoryResult.CacheableFilters);
                cacheEntry = cache.Entries.GetOrAdd(actionDescriptor, cacheEntry);
            }
            else
            {
                filters = FilterFactory.CreateUncachedFilters(
                    _filterProviders,
                    actionContext,
                    cacheEntry.CacheableFilters);
            }

            context.Result = CreateActionInvoker(actionContext, cacheEntry, filters);
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {

        }

        private InnerCache CurrentCache
        {
            get
            {
                var current = _currentCache;
                var actionDescriptors = _collectionProvider.ActionDescriptors;

                if (current == null || current.Version != actionDescriptors.Version)
                {
                    current = new InnerCache(actionDescriptors.Version);
                    _currentCache = current;
                }

                return current;
            }
        }

        private PageActionInvoker CreateActionInvoker(
            ActionContext actionContext,
            PageActionInvokerCacheEntry cacheEntry,
            IFilterMetadata[] filters)
        {
            var tempData = _tempDataFactory.GetTempData(actionContext.HttpContext);
            var pageContext = new PageContext(
                actionContext,
                new ViewDataDictionary(_modelMetadataProvider, actionContext.ModelState),
                tempData,
                _htmlHelperOptions);

            return new PageActionInvoker(
                _selector,
                _diagnosticSource,
                _logger,
                pageContext,
                filters,
                new CopyOnWriteList<IValueProviderFactory>(_valueProviderFactories),
                cacheEntry);
        }

        private PageActionInvokerCacheEntry CreateCacheEntry(
            ActionInvokerProviderContext context,
            FilterItem[] cachedFilters)
        {
            var actionDescriptor = (PageActionDescriptor)context.ActionContext.ActionDescriptor;
            var compiledType = _loader.Load(actionDescriptor).GetTypeInfo();
            var modelType = compiledType.GetProperty(ModelPropertyName)?.PropertyType.GetTypeInfo();

            var compiledActionDescriptor = new CompiledPageActionDescriptor(actionDescriptor)
            {
                ModelTypeInfo = modelType,
                PageTypeInfo = compiledType,
            };

            var pageFactory = _pageFactoryProvider.CreatePageFactory(compiledActionDescriptor);
            var pageDisposer = _pageFactoryProvider.CreatePageDisposer(compiledActionDescriptor);

            Func<PageContext, object> modelFactory = null;
            Action<PageContext, object> modelReleaser = null;
            if (modelType != null)
            {
                modelFactory = _modelFactoryProvider.CreateModelFactory(compiledActionDescriptor);
                modelReleaser = _modelFactoryProvider.CreateModelDisposer(compiledActionDescriptor);
            }

            return new PageActionInvokerCacheEntry(
                compiledActionDescriptor,
                pageFactory,
                pageDisposer,
                modelFactory,
                modelReleaser,
                cachedFilters);
        }

        private class InnerCache
        {
            public InnerCache(int version)
            {
                Version = version;
            }

            public ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry> Entries { get; } =
                new ConcurrentDictionary<ActionDescriptor, PageActionInvokerCacheEntry>();

            public int Version { get; }
        }
    }
}
