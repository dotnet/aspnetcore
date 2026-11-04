// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

#pragma warning disable CA1852 // Seal internal types
internal class DynamicPageEndpointSelectorCache
#pragma warning restore CA1852 // Seal internal types
{
    private readonly ConcurrentDictionary<int, EndpointDataSource> _dataSourceCache = new();
    private readonly ConcurrentDictionary<int, DynamicPageEndpointSelector> _endpointSelectorCache = new();

    public void AddDataSource(PageActionEndpointDataSource dataSource)
    {
        _dataSourceCache.GetOrAdd(dataSource.DataSourceId, dataSource);
    }

    // For testing purposes only
    internal void AddDataSource(EndpointDataSource dataSource, int key) =>
        _dataSourceCache.GetOrAdd(key, dataSource);

    public DynamicPageEndpointSelector? GetEndpointSelector(Endpoint endpoint)
    {
        if (endpoint?.Metadata == null)
        {
            return null;
        }

        var dataSourceId = endpoint.Metadata.GetMetadata<PageEndpointDataSourceIdMetadata>();
        Debug.Assert(dataSourceId is not null);
        return _endpointSelectorCache.GetOrAdd(dataSourceId.Id, EnsureDataSource);
    }

    private DynamicPageEndpointSelector EnsureDataSource(int key)
    {
        if (!_dataSourceCache.TryGetValue(key, out var dataSource))
        {
            throw new InvalidOperationException($"Data source with key '{key}' not registered.");
        }

        return new DynamicPageEndpointSelector(dataSource);
    }
}
