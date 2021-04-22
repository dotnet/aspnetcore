using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DynamicPageEndpointSelectorCache
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

        public DynamicPageEndpointSelector GetEndpointSelector(Endpoint endpoint)
        {
            if (endpoint?.Metadata == null)
            {
                return null;
            }

            var dataSourceId = endpoint.Metadata.GetMetadata<PageEndpointDataSourceIdMetadata>();
            return _endpointSelectorCache.GetOrAdd(dataSourceId.Id, key => EnsureDataSource(key));
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
}
