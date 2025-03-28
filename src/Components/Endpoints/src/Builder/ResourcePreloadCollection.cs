// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class ResourcePreloadCollection
{
    private readonly Dictionary<string?, StringValues> _storage = new();

    public ResourcePreloadCollection(ResourceAssetCollection assets)
    {
        if (assets != null)
        {
            var headers = new List<(string? Order, string Value)>();
            foreach (var asset in assets)
            {
                if (asset.Properties == null)
                {
                    continue;
                }

                // Use preloadgroup=webassembly to identify assets that should to be preloaded
                string? header = null;
                string? group = null;
                foreach (var property in asset.Properties)
                {
                    if (property.Name.Equals("preloadgroup", StringComparison.OrdinalIgnoreCase))
                    {
                        group = property.Value;
                        header = $"<{asset.Url}>";
                        break;
                    }
                }

                if (header == null)
                {
                    continue;
                }

                string? order = null;
                foreach (var property in asset.Properties)
                {
                    if (property.Name.Equals("preloadrel", StringComparison.OrdinalIgnoreCase))
                    {
                        header = String.Concat(header, "; rel=", property.Value);
                    }
                    else if (property.Name.Equals("preloadas", StringComparison.OrdinalIgnoreCase))
                    {
                        header = String.Concat(header, "; as=", property.Value);
                    }
                    else if (property.Name.Equals("preloadpriority", StringComparison.OrdinalIgnoreCase))
                    {
                        header = String.Concat(header, "; fetchpriority=", property.Value);
                    }
                    else if (property.Name.Equals("preloadcrossorigin", StringComparison.OrdinalIgnoreCase))
                    {
                        header = String.Concat(header, "; crossorigin=", property.Value);
                    }
                    else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
                    {
                        header = String.Concat(header, "; integrity=\"", property.Value, "\"");
                    }
                    else if (property.Name.Equals("preloadorder", StringComparison.OrdinalIgnoreCase))
                    {
                        order = property.Value;
                    }
                }

                if (header != null)
                {
                    headers.Add((order, header));
                }
            }

            headers.Sort((a, b) => string.Compare(a.Order, b.Order, StringComparison.InvariantCulture));
        }
    }

    public bool TryGetLinkHeaders(string group, out StringValues linkHeaders)
        => _storage.TryGetValue(group, out linkHeaders);
}
