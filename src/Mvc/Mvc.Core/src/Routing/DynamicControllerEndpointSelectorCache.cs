// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing;

#pragma warning disable CA1852 // Seal internal types
internal class DynamicControllerEndpointSelectorCache
#pragma warning restore CA1852 // Seal internal types
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
        return _endpointSelectorCache.GetOrAdd(dataSourceId.Id, EnsureDataSource);
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
