// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Describes a mapping of static assets to their corresponding unique URLs.
/// </summary>
public sealed class ResourceAssetCollection : IReadOnlyList<ResourceAsset>
{
    /// <summary>
    /// An empty <see cref="ResourceAssetCollection"/>.
    /// </summary>
    public static readonly ResourceAssetCollection Empty = new([]);

    private readonly Dictionary<string, ResourceAsset> _uniqueUrlMappings;
    private readonly HashSet<string> _contentSpecificUrls;
    private readonly IReadOnlyList<ResourceAsset> _resources;

    /// <summary>
    /// Initializes a new instance of <see cref="ResourceAssetCollection"/>
    /// </summary>
    /// <param name="resources">The list of resources available.</param>
    public ResourceAssetCollection(IReadOnlyList<ResourceAsset> resources)
    {
        _uniqueUrlMappings = new Dictionary<string, ResourceAsset>(StringComparer.OrdinalIgnoreCase);
        _contentSpecificUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _resources = resources;
        foreach (var resource in resources)
        {
            foreach (var property in resource.Properties ?? [])
            {
                if (property.Name.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    _uniqueUrlMappings[property.Value] = resource;
                    _contentSpecificUrls.Add(resource.Url);
                }
            }
        }
    }

    /// <summary>
    /// Gets the unique content-based URL for the specified static asset.
    /// </summary>
    /// <param name="key">The asset name.</param>
    /// <returns>The unique URL if availabe, the same <paramref name="key"/> if not available.</returns>
    public string this[string key]
    {
        get
        {
            if (_uniqueUrlMappings.TryGetValue(key, out var value))
            {
                return value.Url;
            }

            return key;
        }
    }

    /// <summary>
    /// Determines whether the specified path is a content-specific URL.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path is a content-specific URL; otherwise, <c>false</c>.</returns>
    public bool IsContentSpecificUrl(string path)
    {
        return _contentSpecificUrls.Contains(path);
    }

    ResourceAsset IReadOnlyList<ResourceAsset>.this[int index] => _resources[index];
    int IReadOnlyCollection<ResourceAsset>.Count => _resources.Count;
    IEnumerator<ResourceAsset> IEnumerable<ResourceAsset>.GetEnumerator() => _resources.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();
}
