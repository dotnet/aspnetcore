// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerProvider : IActionInvokerProvider
    {
        private const string ModelPropertyName = "Model";
        private readonly IPageLoader _loader;
        private readonly IPageFactoryProvider _pageFactoryProvider;
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IFilterProvider[] _filterProviders;
        private volatile InnerCache _currentCache;

        public PageActionInvokerProvider(
            IPageLoader loader,
            IPageFactoryProvider pageFactoryProvider,
            IActionDescriptorCollectionProvider collectionProvider,
            IEnumerable<IFilterProvider> filterProviders)
        {
            _loader = loader;
            _collectionProvider = collectionProvider;
            _pageFactoryProvider = pageFactoryProvider;
            _filterProviders = filterProviders.ToArray();
        }

        public int Order { get; } = -1000;

        public void OnProvidersExecuting(ActionInvokerProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var actionDescriptor = context.ActionContext.ActionDescriptor as PageActionDescriptor;
            if (actionDescriptor == null)
            {
                return;
            }

            var cache = CurrentCache;
            PageActionInvokerCacheEntry cacheEntry;

            IFilterMetadata[] filters;
            if (!cache.Entries.TryGetValue(actionDescriptor, out cacheEntry))
            {
                var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, context.ActionContext);
                filters = filterFactoryResult.Filters;
                cacheEntry = CreateCacheEntry(context, filterFactoryResult.CacheableFilters);
                cacheEntry = cache.Entries.GetOrAdd(actionDescriptor, cacheEntry);
            }
            else
            {
                filters = FilterFactory.CreateUncachedFilters(_filterProviders, context.ActionContext, cacheEntry.CacheableFilters);
            }

            context.Result = new PageActionInvoker(cacheEntry, context.ActionContext, filters);
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

        private PageActionInvokerCacheEntry CreateCacheEntry(ActionInvokerProviderContext context, FilterItem[] filters)
        {
            var actionDescriptor = (PageActionDescriptor)context.ActionContext.ActionDescriptor;
            var compiledType = _loader.Load(actionDescriptor).GetTypeInfo();
            var modelType = compiledType.GetProperty(ModelPropertyName)?.PropertyType.GetTypeInfo();

            var compiledActionDescriptor = new CompiledPageActionDescriptor(actionDescriptor)
            {
                ModelTypeInfo = modelType,
                PageTypeInfo = compiledType,
            };

            return new PageActionInvokerCacheEntry(
                compiledActionDescriptor,
                _pageFactoryProvider.CreatePageFactory(compiledActionDescriptor),
                _pageFactoryProvider.CreatePageDisposer(compiledActionDescriptor),
                filters);
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
