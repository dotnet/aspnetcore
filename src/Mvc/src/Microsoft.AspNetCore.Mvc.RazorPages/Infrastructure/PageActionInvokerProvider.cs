// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class PageActionInvokerProvider : IActionInvokerProvider
    {
        private readonly PageLoaderBase _loader;
        private readonly IPageFactoryProvider _pageFactoryProvider;
        private readonly IPageModelFactoryProvider _modelFactoryProvider;
        private readonly IRazorPageFactoryProvider _razorPageFactoryProvider;
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IFilterProvider[] _filterProviders;
        private readonly ParameterBinder _parameterBinder;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IPageActionInvokerFactory _pageActionInvokerFactory;

        private volatile InnerCache _currentCache;

        public PageActionInvokerProvider(
            PageLoaderBase loader,
            IPageFactoryProvider pageFactoryProvider,
            IPageModelFactoryProvider modelFactoryProvider,
            IRazorPageFactoryProvider razorPageFactoryProvider,
            IActionDescriptorCollectionProvider collectionProvider,
            IEnumerable<IFilterProvider> filterProviders,
            ParameterBinder parameterBinder,
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IPageActionInvokerFactory pageActionInvokerFactory)
        {
            _loader = loader;
            _pageFactoryProvider = pageFactoryProvider;
            _modelFactoryProvider = modelFactoryProvider;
            _razorPageFactoryProvider = razorPageFactoryProvider;
            _collectionProvider = collectionProvider;
            _filterProviders = filterProviders.ToArray();
            _parameterBinder = parameterBinder;
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _pageActionInvokerFactory = pageActionInvokerFactory;
        }

        public int Order { get; } = -1000;

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var actionContext = context.ActionContext;
            if (!(actionContext.ActionDescriptor is PageActionDescriptor actionDescriptor))
            {
                return;
            }

            var cache = CurrentCache;
            if (!cache.Entries.TryGetValue(actionDescriptor, out var cacheEntry))
            {
                var createPageCacheInvoker = new CreatePageCacheEntryActionInvoker(
                    _loader,
                    _pageFactoryProvider,
                    _modelFactoryProvider,
                    _razorPageFactoryProvider,
                    _filterProviders,
                    _parameterBinder,
                    _modelBinderFactory,
                    _modelMetadataProvider,
                    _pageActionInvokerFactory,
                    cache.Entries,
                    actionContext,
                    actionDescriptor);

                context.Result = createPageCacheInvoker;
            }
            else
            {
                var filters = FilterFactory.CreateUncachedFilters(
                    _filterProviders,
                    actionContext,
                    cacheEntry.CacheableFilters);

                context.Result = _pageActionInvokerFactory.CreateInvoker(actionContext, cacheEntry, filters);
            }
        }

        public void OnProvidersExecuted(ActionInvokerProviderContext context)
        {

        }

        internal InnerCache CurrentCache
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

        internal class InnerCache
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
