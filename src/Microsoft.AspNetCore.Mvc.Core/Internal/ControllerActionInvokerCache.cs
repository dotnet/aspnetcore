// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerCache
    {
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IFilterProvider[] _filterProviders;

        private volatile InnerCache _currentCache;

        public ControllerActionInvokerCache(
            IActionDescriptorCollectionProvider collectionProvider,
            IEnumerable<IFilterProvider> filterProviders)
        {
            _collectionProvider = collectionProvider;
            _filterProviders = filterProviders.OrderBy(item => item.Order).ToArray();
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

        public ControllerActionInvokerState GetState(ControllerContext controllerContext)
        {
            var cache = CurrentCache;
            var actionDescriptor = controllerContext.ActionDescriptor;

            IFilterMetadata[] filters;
            Entry cacheEntry;
            if (!cache.Entries.TryGetValue(actionDescriptor, out cacheEntry))
            {
                var filterFactoryResult = FilterFactory.GetAllFilters(_filterProviders, controllerContext);
                filters = filterFactoryResult.Filters;

                var parameterDefaultValues = ParameterDefaultValues
                    .GetParameterDefaultValues(actionDescriptor.MethodInfo);

                var executor = ObjectMethodExecutor.Create(
                    actionDescriptor.MethodInfo,
                    actionDescriptor.ControllerTypeInfo,
                    parameterDefaultValues);

                cacheEntry = new Entry(filterFactoryResult.CacheableFilters, executor);
                cacheEntry = cache.Entries.GetOrAdd(actionDescriptor, cacheEntry);
            }
            else
            {
                // Filter instances from statically defined filter descriptors + from filter providers
                filters = FilterFactory.CreateUncachedFilters(_filterProviders, controllerContext, cacheEntry.FilterItems);
            }

            return new ControllerActionInvokerState(filters, cacheEntry.ActionMethodExecutor);
        }

        private class InnerCache
        {
            public InnerCache(int version)
            {
                Version = version;
            }

            public ConcurrentDictionary<ActionDescriptor, Entry> Entries { get; } =
                new ConcurrentDictionary<ActionDescriptor, Entry>();

            public int Version { get; }
        }

        private struct Entry
        {
            public Entry(FilterItem[] items, ObjectMethodExecutor executor)
            {
                FilterItems = items;
                ActionMethodExecutor = executor;
            }

            public FilterItem[] FilterItems { get; }

            public ObjectMethodExecutor ActionMethodExecutor { get; }
        }

        public struct ControllerActionInvokerState
        {
            public ControllerActionInvokerState(
                IFilterMetadata[] filters,
                ObjectMethodExecutor actionMethodExecutor)
            {
                Filters = filters;
                ActionMethodExecutor = actionMethodExecutor;
            }

            public IFilterMetadata[] Filters { get; }

            public ObjectMethodExecutor ActionMethodExecutor { get; set; }
        }
    }
}
