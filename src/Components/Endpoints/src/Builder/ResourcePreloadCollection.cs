// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class ResourcePreloadCollection
{
    private readonly Dictionary<string, List<PreloadAsset>> _storage = new();

    public ResourcePreloadCollection(ResourceAssetCollection assets)
    {
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

            var preloadAsset = CreateAsset(asset.Url, asset.Properties);
            if (!_storage.TryGetValue(group, out var groupHeaders))
            {
                groupHeaders = _storage[group] = new List<PreloadAsset>();
            }

            groupHeaders.Add(preloadAsset);
        }

        foreach (var group in _storage)
        {
            group.Value.Sort((a, b) => a.PreloadOrder.CompareTo(b.PreloadOrder));
        }
    }

    private static PreloadAsset CreateAsset(string url, IEnumerable<ResourceAssetProperty> properties)
    {
        var resourceAsset = new PreloadAsset(url);
        foreach (var property in properties)
        {
            if (property.Name.Equals("label", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.Label = property.Value;
            }
            else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.Integrity = property.Value;
            }
            else if (property.Name.Equals("preloadgroup", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.PreloadGroup = property.Value;
            }
            else if (property.Name.Equals("preloadrel", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.PreloadRel = property.Value;
            }
            else if (property.Name.Equals("preloadas", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.PreloadAs = property.Value;
            }
            else if (property.Name.Equals("preloadpriority", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.PreloadPriority = property.Value;
            }
            else if (property.Name.Equals("preloadcrossorigin", StringComparison.OrdinalIgnoreCase))
            {
                resourceAsset.PreloadCrossorigin = property.Value;
            }
            else if (property.Name.Equals("preloadorder", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(property.Value, out int order))
                {
                    order = 0;
                }

                resourceAsset.PreloadOrder = order;
            }
        }

        return resourceAsset;
    }

    public bool TryGetAssets(string group, [MaybeNullWhen(false)] out List<PreloadAsset> assets)
        => _storage.TryGetValue(group, out assets);
}

internal sealed class PreloadAsset(string url)
{
    public string Url { get; } = url;
    public string? Label { get; set; }
    public string? Integrity { get; set; }
    public string? PreloadGroup { get; set; }
    public string? PreloadRel { get; set; }
    public string? PreloadAs { get; set; }
    public string? PreloadPriority { get; set; }
    public string? PreloadCrossorigin { get; set; }
    public int PreloadOrder { get; set; }
}
