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
        if (assets != null)
        {
            var headers = new List<(string? Group, string? Order, string Value)>();
            foreach (var asset in assets)
            {
                if (asset.Properties == null)
                {
                    continue;
                }

                // Use preloadgroup=webassembly to identify assets that should to be preloaded
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

                var header = new StringBuilder();
                header.Append('<');
                header.Append(asset.Url);
                header.Append('>');

                string? order = null;
                foreach (var property in asset.Properties)
                {
                    if (property.Name.Equals("preloadrel", StringComparison.OrdinalIgnoreCase))
                    {
                        header.Append("; rel=").Append(property.Value);
                    }
                    else if (property.Name.Equals("preloadas", StringComparison.OrdinalIgnoreCase))
                    {
                        header.Append("; as=").Append(property.Value);
                    }
                    else if (property.Name.Equals("preloadpriority", StringComparison.OrdinalIgnoreCase))
                    {
                        header.Append("; fetchpriority=").Append(property.Value);
                    }
                    else if (property.Name.Equals("preloadcrossorigin", StringComparison.OrdinalIgnoreCase))
                    {
                        header.Append("; crossorigin=").Append(property.Value);
                    }
                    else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
                    {
                        header.Append("; integrity=\"").Append(property.Value).Append('"');
                    }
                    else if (property.Name.Equals("preloadorder", StringComparison.OrdinalIgnoreCase))
                    {
                        order = property.Value;
                    }
                }

                if (header != null)
                {
                    headers.Add((group, order, header.ToString()));
                }
            }

            foreach (var group in headers.GroupBy(h => h.Group))
            {
                _storage[group.Key ?? string.Empty] = group.OrderBy(h => h.Order).Select(h => h.Value).ToArray();
            }
        }
    }

    public bool TryGetLinkHeaders(string group, out StringValues linkHeaders)
        => _storage.TryGetValue(group, out linkHeaders);
}
