// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class DynamicControllerEndpointSelectorCache
    {
        private readonly ConcurrentDictionary<int, EndpointDataSource> _dataSourceCache = new();
        private readonly ConcurrentDictionary<int, DynamicControllerEndpointSelector> _endpointSelectorCache = new();

        public void AddDataSource(ControllerActionEndpointDataSource dataSource)
        {
            _dataSourceCache.GetOrAdd(dataSource.DataSourceId, dataSource);
        }

        // For testing purposes only
        internal void AddDataSource(EndpointDataSource dataSource, int key) =>
            _dataSourceCache.GetOrAdd(key, dataSource);

        public DynamicControllerEndpointSelector GetEndpointSelector(Endpoint endpoint)
        {
            var dataSourceId = endpoint.Metadata.GetMetadata<ControllerEndpointDataSourceIdMetadata>()!;
            return _endpointSelectorCache.GetOrAdd(dataSourceId.Id, key => EnsureDataSource(key));
        }

        private DynamicControllerEndpointSelector EnsureDataSource(int key)
        {
            if (!_dataSourceCache.TryGetValue(key, out var dataSource))
            {
                throw new InvalidOperationException($"Data source with key '{key}' not registered.");
            }

            return new DynamicControllerEndpointSelector(dataSource);
        }
    }
}
