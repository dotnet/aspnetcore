// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
#pragma warning disable CA1852 // Seal internal types
internal class ResourceManagerStringProvider : IResourceStringProvider
#pragma warning restore CA1852 // Seal internal types
{
    private readonly IResourceNamesCache _resourceNamesCache;
    private readonly ResourceManager _resourceManager;
    private readonly Assembly _assembly;
    private readonly string _resourceBaseName;

    public ResourceManagerStringProvider(
        IResourceNamesCache resourceCache,
        ResourceManager resourceManager,
        Assembly assembly,
        string baseName)
    {
        _resourceManager = resourceManager;
        _resourceNamesCache = resourceCache;
        _assembly = assembly;
        _resourceBaseName = baseName;
    }

    private string GetResourceCacheKey(CultureInfo culture)
    {
        var resourceName = _resourceManager.BaseName;

        return $"Culture={culture.Name};resourceName={resourceName};Assembly={_assembly.FullName}";
    }

    private string GetResourceName(CultureInfo culture)
    {
        var resourceStreamName = _resourceBaseName;
        if (!string.IsNullOrEmpty(culture.Name))
        {
            resourceStreamName += "." + culture.Name;
        }
        resourceStreamName += ".resources";

        return resourceStreamName;
    }

    public IList<string>? GetAllResourceStrings(CultureInfo culture, bool throwOnMissing)
    {
        var cacheKey = GetResourceCacheKey(culture);

        return _resourceNamesCache.GetOrAdd(cacheKey, _ =>
        {
            // We purposly don't dispose the ResourceSet because it causes an ObjectDisposedException when you try to read the values later.
            var resourceSet = _resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
            if (resourceSet == null)
            {
                if (throwOnMissing)
                {
                    throw new MissingManifestResourceException(Resources.FormatLocalization_MissingManifest(GetResourceName(culture)));
                }
                else
                {
                    return null;
                }
            }

            var names = new List<string>();
            foreach (DictionaryEntry? entry in resourceSet)
            {
                if (entry?.Key is string key)
                {
                    names.Add(key);
                }
            }

            return names;
        });
    }
}
