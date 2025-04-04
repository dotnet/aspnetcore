// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class ResourcePreloadCollection
{
    private readonly Dictionary<string, StringValues> _storage = new();

    public ResourcePreloadCollection(ResourceAssetCollection assets)
    {
        var headerBuilder = new StringBuilder();
        var headers = new Dictionary<string, List<(int Order, string Value)>>();
        foreach (var asset in assets)
        {
            if (asset.Properties == null)
            {
                continue;
            }

            // Use preloadgroup property to identify assets that should be preloaded
            string? group = null;
            foreach (var property in asset.Properties)
            {
                if (property.Name.Equals("preloadgroup", StringComparison.OrdinalIgnoreCase))
                {
                    group = property.Value ?? string.Empty;
                    break;
                }
            }

            if (group == null)
            {
                continue;
            }

            var header = CreateHeader(headerBuilder, asset.Url, asset.Properties);
            if (!headers.TryGetValue(group, out var groupHeaders))
            {
                groupHeaders = headers[group] = new List<(int Order, string Value)>();
            }

            groupHeaders.Add(header);
        }

        foreach (var group in headers)
        {
            _storage[group.Key ?? string.Empty] = group.Value.OrderBy(h => h.Order).Select(h => h.Value).ToArray();
        }
    }

    private static (int order, string header) CreateHeader(StringBuilder headerBuilder, string url, IEnumerable<ResourceAssetProperty> properties)
    {
        headerBuilder.Clear();
        headerBuilder.Append('<');
        headerBuilder.Append(url);
        headerBuilder.Append('>');

        int order = 0;
        foreach (var property in properties)
        {
            if (property.Name.Equals("preloadrel", StringComparison.OrdinalIgnoreCase))
            {
                headerBuilder.Append("; rel=").Append(property.Value);
            }
            else if (property.Name.Equals("preloadas", StringComparison.OrdinalIgnoreCase))
            {
                headerBuilder.Append("; as=").Append(property.Value);
            }
            else if (property.Name.Equals("preloadpriority", StringComparison.OrdinalIgnoreCase))
            {
                headerBuilder.Append("; fetchpriority=").Append(property.Value);
            }
            else if (property.Name.Equals("preloadcrossorigin", StringComparison.OrdinalIgnoreCase))
            {
                headerBuilder.Append("; crossorigin=").Append(property.Value);
            }
            else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
            {
                headerBuilder.Append("; integrity=\"").Append(property.Value).Append('"');
            }
            else if (property.Name.Equals("preloadorder", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(property.Value, out order))
                {
                    order = 0;
                }
            }
        }

        return (order, headerBuilder.ToString());
    }

    public bool TryGetLinkHeaders(string group, out StringValues linkHeaders)
        => _storage.TryGetValue(group, out linkHeaders);
}
