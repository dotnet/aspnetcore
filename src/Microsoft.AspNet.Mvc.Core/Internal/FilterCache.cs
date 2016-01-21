// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace Microsoft.AspNet.Mvc.Internal
{
    public class FilterCache
    {
        private readonly IFilterMetadata[] EmptyFilterArray = new IFilterMetadata[0];

        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IFilterProvider[] _filterProviders;

        private volatile InnerCache _currentCache;

        public FilterCache(
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

        public IFilterMetadata[] GetFilters(ActionContext actionContext)
        {
            var cache = CurrentCache;
            var actionDescriptor = actionContext.ActionDescriptor;

            CacheEntry entry;
            if (cache.Entries.TryGetValue(actionDescriptor, out entry))
            {
                return GetFiltersFromEntry(entry, actionContext);
            }

            var items = new List<FilterItem>(actionDescriptor.FilterDescriptors.Count);
            for (var i = 0; i < actionDescriptor.FilterDescriptors.Count; i++)
            {
                items.Add(new FilterItem(actionDescriptor.FilterDescriptors[i]));
            }

            ExecuteProviders(actionContext, items);

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
                entry = new CacheEntry(filters);
            }
            else
            {
                entry = new CacheEntry(items);
            }

            cache.Entries.TryAdd(actionDescriptor, entry);
            return filters;
        }

        private IFilterMetadata[] GetFiltersFromEntry(CacheEntry entry, ActionContext actionContext)
        {
            Debug.Assert(entry.Filters != null || entry.Items != null);

            if (entry.Filters != null)
            {
                return entry.Filters;
            }

            var items = new List<FilterItem>(entry.Items.Count);
            for (var i = 0; i < entry.Items.Count; i++)
            {
                var item = entry.Items[i];
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
                for (int i = 0, j = 0; i < items.Count; i++)
                {
                    var filter = items[i].Filter;
                    if (filter != null)
                    {
                        filters[j++] = filter;
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

            public ConcurrentDictionary<ActionDescriptor, CacheEntry> Entries { get; } = 
                new ConcurrentDictionary<ActionDescriptor, CacheEntry>();

            public int Version { get; }
        }

        private struct CacheEntry
        {
            public CacheEntry(IFilterMetadata[] filters)
            {
                Filters = filters;
                Items = null;
            }

            public CacheEntry(List<FilterItem> items)
            {
                Items = items;
                Filters = null;
            }

            public IFilterMetadata[] Filters { get; }

            public List<FilterItem> Items { get; }
        }
    }
}
