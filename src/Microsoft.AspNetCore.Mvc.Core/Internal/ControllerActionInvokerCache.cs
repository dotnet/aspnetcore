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

        public Entry GetCacheEntry(ControllerContext controllerContext)
        {
            var cache = CurrentCache;
            var actionDescriptor = controllerContext.ActionDescriptor;

            Entry entry;
            if (cache.Entries.TryGetValue(actionDescriptor, out entry))
            {
                return entry;
            }

            var executor = ObjectMethodExecutor.Create(actionDescriptor.MethodInfo, actionDescriptor.ControllerTypeInfo);

            var items = new List<FilterItem>(actionDescriptor.FilterDescriptors.Count);
            for (var i = 0; i < actionDescriptor.FilterDescriptors.Count; i++)
            {
                items.Add(new FilterItem(actionDescriptor.FilterDescriptors[i]));
            }

            ExecuteProviders(controllerContext, items);

            var filters = ExtractFilters(items);

            var allFiltersCached = true;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.IsReusable)
                {
                    item.Filter = null;
                    allFiltersCached = false;
                }
            }

            if (allFiltersCached)
            {
                entry = new Entry(filters, null, executor);
            }
            else
            {
                entry = new Entry(null, items, executor);
            }

            cache.Entries.TryAdd(actionDescriptor, entry);
            return entry;
        }

        public IFilterMetadata[] GetFilters(ControllerContext controllerContext)
        {
            var entry = GetCacheEntry(controllerContext);
            return GetFiltersFromEntry(entry, controllerContext);
        }

        public ObjectMethodExecutor GetControllerActionMethodExecutor(ControllerContext controllerContext)
        {
            var entry = GetCacheEntry(controllerContext);
            return entry.ActionMethodExecutor;
        }

        public IFilterMetadata[] GetFiltersFromEntry(Entry entry, ActionContext actionContext)
        {
            Debug.Assert(entry.Filters != null || entry.FilterItems != null);

            if (entry.Filters != null)
            {
                return entry.Filters;
            }

            var items = new List<FilterItem>(entry.FilterItems.Count);
            for (var i = 0; i < entry.FilterItems.Count; i++)
            {
                var item = entry.FilterItems[i];
                if (item.IsReusable)
                {
                    items.Add(item);
                }
                else
                {
                    items.Add(new FilterItem(item.Descriptor));
                }
            }

            ExecuteProviders(actionContext, items);

            return ExtractFilters(items);
        }

        private void ExecuteProviders(ActionContext actionContext, List<FilterItem> items)
        {
            var context = new FilterProviderContext(actionContext, items);

            for (var i = 0; i < _filterProviders.Length; i++)
            {
                _filterProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _filterProviders.Length - 1; i >= 0; i--)
            {
                _filterProviders[i].OnProvidersExecuted(context);
            }
        }

        private IFilterMetadata[] ExtractFilters(List<FilterItem> items)
        {
            var count = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Filter != null)
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
                for (int i = 0; i < items.Count; i++)
                {
                    var filter = items[i].Filter;
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

        public struct Entry
        {
            public Entry(IFilterMetadata[] filters, List<FilterItem> items, ObjectMethodExecutor executor)
            {
                FilterItems = items;
                Filters = filters;
                ActionMethodExecutor = executor;
            }
            public IFilterMetadata[] Filters { get; }

            public List<FilterItem> FilterItems { get; }

            public ObjectMethodExecutor ActionMethodExecutor { get; }
        }
    }
}
