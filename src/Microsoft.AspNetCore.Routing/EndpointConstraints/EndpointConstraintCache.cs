// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.EndpointConstraints
{
    internal class EndpointConstraintCache
    {
        private readonly CompositeEndpointDataSource _dataSource;
        private readonly IEndpointConstraintProvider[] _endpointConstraintProviders;

        private volatile InnerCache _currentCache;

        public EndpointConstraintCache(
            CompositeEndpointDataSource dataSource,
            IEnumerable<IEndpointConstraintProvider> endpointConstraintProviders)
        {
            _dataSource = dataSource;
            _endpointConstraintProviders = endpointConstraintProviders.OrderBy(item => item.Order).ToArray();
        }

        private InnerCache CurrentCache
        {
            get
            {
                var current = _currentCache;
                var endpointDescriptors = _dataSource.Endpoints;

                if (current == null)
                {
                    current = new InnerCache();
                    _currentCache = current;
                }

                return current;
            }
        }

        public IReadOnlyList<IEndpointConstraint> GetEndpointConstraints(HttpContext httpContext, Endpoint endpoint)
        {
            var cache = CurrentCache;

            if (cache.Entries.TryGetValue(endpoint, out var entry))
            {
                return GetEndpointConstraintsFromEntry(entry, httpContext, endpoint);
            }

            if (endpoint.Metadata == null || endpoint.Metadata.Count == 0)
            {
                return null;
            }

            var items = endpoint.Metadata
                .OfType<IEndpointConstraintMetadata>()
                .Select(m => new EndpointConstraintItem(m))
                .ToList();

            ExecuteProviders(httpContext, endpoint, items);

            var endpointConstraints = ExtractEndpointConstraints(items);

            var allEndpointConstraintsCached = true;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.IsReusable)
                {
                    item.Constraint = null;
                    allEndpointConstraintsCached = false;
                }
            }

            if (allEndpointConstraintsCached)
            {
                entry = new CacheEntry(endpointConstraints);
            }
            else
            {
                entry = new CacheEntry(items);
            }

            cache.Entries.TryAdd(endpoint, entry);
            return endpointConstraints;
        }

        private IReadOnlyList<IEndpointConstraint> GetEndpointConstraintsFromEntry(CacheEntry entry, HttpContext httpContext, Endpoint endpoint)
        {
            Debug.Assert(entry.EndpointConstraints != null || entry.Items != null);

            if (entry.EndpointConstraints != null)
            {
                return entry.EndpointConstraints;
            }

            var items = new List<EndpointConstraintItem>(entry.Items.Count);
            for (var i = 0; i < entry.Items.Count; i++)
            {
                var item = entry.Items[i];
                if (item.IsReusable)
                {
                    items.Add(item);
                }
                else
                {
                    items.Add(new EndpointConstraintItem(item.Metadata));
                }
            }

            ExecuteProviders(httpContext, endpoint, items);

            return ExtractEndpointConstraints(items);
        }

        private void ExecuteProviders(HttpContext httpContext, Endpoint endpoint, List<EndpointConstraintItem> items)
        {
            var context = new EndpointConstraintProviderContext(httpContext, endpoint, items);

            for (var i = 0; i < _endpointConstraintProviders.Length; i++)
            {
                _endpointConstraintProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _endpointConstraintProviders.Length - 1; i >= 0; i--)
            {
                _endpointConstraintProviders[i].OnProvidersExecuted(context);
            }
        }

        private IReadOnlyList<IEndpointConstraint> ExtractEndpointConstraints(List<EndpointConstraintItem> items)
        {
            var count = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Constraint != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return null;
            }

            var endpointConstraints = new IEndpointConstraint[count];
            var endpointConstraintIndex = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var endpointConstraint = items[i].Constraint;
                if (endpointConstraint != null)
                {
                    endpointConstraints[endpointConstraintIndex++] = endpointConstraint;
                }
            }

            return endpointConstraints;
        }

        private class InnerCache
        {
            public InnerCache()
            {
            }

            public ConcurrentDictionary<Endpoint, CacheEntry> Entries { get; } =
                new ConcurrentDictionary<Endpoint, CacheEntry>();
        }

        private struct CacheEntry
        {
            public CacheEntry(IReadOnlyList<IEndpointConstraint> endpointConstraints)
            {
                EndpointConstraints = endpointConstraints;
                Items = null;
            }

            public CacheEntry(List<EndpointConstraintItem> items)
            {
                Items = items;
                EndpointConstraints = null;
            }

            public IReadOnlyList<IEndpointConstraint> EndpointConstraints { get; }

            public List<EndpointConstraintItem> Items { get; }
        }
    }
}