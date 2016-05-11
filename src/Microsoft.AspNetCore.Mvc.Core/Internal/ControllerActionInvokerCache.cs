// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerCache
    {
        private readonly IFilterMetadata[] EmptyFilterArray = new IFilterMetadata[0];

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
            // Filter instances from statically defined filter descriptors + from filter providers
            IFilterMetadata[] filters;

            var cache = CurrentCache;
            var actionDescriptor = controllerContext.ActionDescriptor;

            Entry cacheEntry;
            if (cache.Entries.TryGetValue(actionDescriptor, out cacheEntry))
            {
                // Deep copy the cached filter items as filter providers could modify them
                var filterItems = new List<FilterItem>(cacheEntry.FilterItems.Count);
                for (var i = 0; i < cacheEntry.FilterItems.Count; i++)
                {
                    var filterItem = cacheEntry.FilterItems[i];
                    filterItems.Add(
                        new FilterItem(filterItem.Descriptor)
                        {
                            Filter = filterItem.Filter,
                            IsReusable = filterItem.IsReusable
                        });
                }

                filters = GetFilters(controllerContext, filterItems);

                return new ControllerActionInvokerState(filters, cacheEntry.ActionMethodExecutor);
            }

            var executor = ObjectMethodExecutor.Create(
                actionDescriptor.MethodInfo,
                actionDescriptor.ControllerTypeInfo);

            var staticFilterItems = new List<FilterItem>(actionDescriptor.FilterDescriptors.Count);
            for (var i = 0; i < actionDescriptor.FilterDescriptors.Count; i++)
            {
                staticFilterItems.Add(new FilterItem(actionDescriptor.FilterDescriptors[i]));
            }

            // Create a separate collection as we want to hold onto the statically defined filter items
            // in order to cache them
            var allFilterItems = new List<FilterItem>(staticFilterItems);

            filters = GetFilters(controllerContext, allFilterItems);

            // Cache the filter items based on the following criteria
            // 1. Are created statically (ex: via filter attributes, added to global filter list etc.)
            // 2. Are re-usable
            for (var i = 0; i < staticFilterItems.Count; i++)
            {
                var item = staticFilterItems[i];
                if (!item.IsReusable)
                {
                    item.Filter = null;
                }
            }
            cacheEntry = new Entry(staticFilterItems, executor);
            cache.Entries.TryAdd(actionDescriptor, cacheEntry);

            return new ControllerActionInvokerState(filters, cacheEntry.ActionMethodExecutor);
        }

        private IFilterMetadata[] GetFilters(ActionContext actionContext, List<FilterItem> filterItems)
        {
            // Execute providers
            var context = new FilterProviderContext(actionContext, filterItems);

            for (var i = 0; i < _filterProviders.Length; i++)
            {
                _filterProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _filterProviders.Length - 1; i >= 0; i--)
            {
                _filterProviders[i].OnProvidersExecuted(context);
            }

            // Extract filter instances from statically defined filters and filter providers
            var count = 0;
            for (var i = 0; i < filterItems.Count; i++)
            {
                if (filterItems[i].Filter != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return EmptyFilterArray;
            }
            else
            {
                var filters = new IFilterMetadata[count];
                var filterIndex = 0;
                for (int i = 0; i < filterItems.Count; i++)
                {
                    var filter = filterItems[i].Filter;
                    if (filter != null)
                    {
                        filters[filterIndex++] = filter;
                    }
                }

                return filters;
            }
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
            public Entry(List<FilterItem> items, ObjectMethodExecutor executor)
            {
                FilterItems = items;
                ActionMethodExecutor = executor;
            }

            public List<FilterItem> FilterItems { get; }

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
