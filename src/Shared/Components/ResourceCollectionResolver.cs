// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.AspNetCore.StaticAssets.Infrastructure;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class ResourceCollectionResolver(IEndpointRouteBuilder endpoints)
{
#if !MVC_VIEWFEATURES
#else
    private ResourceAssetCollection _resourceCollection;
    private ImportMapDefinition _importMapDefinition;

    public string ManifestName { get; set; }
#endif

#if !MVC_VIEWFEATURES
    public ResourceAssetCollection ResolveResourceCollection(string? manifestName = null)
    {
        var descriptors = StaticAssetsEndpointDataSourceHelper.ResolveStaticAssetDescriptors(endpoints, manifestName);
#else
    public ResourceAssetCollection ResolveResourceCollection()
    {
        if (_resourceCollection != null)
        {
            return _resourceCollection;
        }

        var descriptors = StaticAssetsEndpointDataSourceHelper.ResolveStaticAssetDescriptors(endpoints, ManifestName);
#endif
        var resources = new List<ResourceAsset>();

        // We are converting a subset of the descriptors to resources and including a subset of the properties exposed by the
        // descriptors that are useful for the resources in the context of Blazor. Specifically, we pass in the `label` property
        // which contains the human-readable identifier for fingerprinted assets, and the integrity, which can be used to apply
        // subresource integrity to things like images, script tags, etc.
        foreach (var descriptor in descriptors)
        {
#if !MVC_VIEWFEATURES
            string? label = null;
            string? integrity = null;
#else
            string label = null;
            string integrity = null;
#endif

            // If there's a selector this means that this is an alternative representation for a resource, so skip it.
            if (descriptor.Selectors.Count == 0)
            {
                var foundProperties = 0;
                for (var i = 0; i < descriptor.Properties.Count; i++)
                {
                    var property = descriptor.Properties[i];
                    if (property.Name.Equals("label", StringComparison.OrdinalIgnoreCase))
                    {
                        label = property.Value;
                        foundProperties++;
                    }

                    else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
                    {
                        integrity = property.Value;
                        foundProperties++;
                    }
                }

                AddResource(resources, descriptor, label, integrity, foundProperties);
            }
        }

        // Sort the resources because we are going to generate a hash for the collection to use when we expose it as an endpoint
        // for webassembly to consume. This way, we can cache this collection forever until it changes.
        resources.Sort((a, b) => string.Compare(a.Url, b.Url, StringComparison.Ordinal));

        var result = new ResourceAssetCollection(resources);
#if MVC_VIEWFEATURES
        _resourceCollection = result;
#endif
        return result;
    }

#if !MVC_VIEWFEATURES
    public bool IsRegistered(string? manifestName = null)
#else
    public bool IsRegistered(string manifestName = null)
#endif
    {
        return StaticAssetsEndpointDataSourceHelper.HasStaticAssetsDataSource(endpoints, manifestName);
    }

    private static void AddResource(
        List<ResourceAsset> resources,
        StaticAssetDescriptor descriptor,
#if !MVC_VIEWFEATURES
        string? label,
        string? integrity,
#else
        string label,
        string integrity,
#endif
        int foundProperties)
    {
        if (label != null || integrity != null)
        {
            var properties = new ResourceAssetProperty[foundProperties];
            var index = 0;
            if (label != null)
            {
                properties[index++] = new ResourceAssetProperty("label", label);
            }
            if (integrity != null)
            {
                properties[index++] = new ResourceAssetProperty("integrity", integrity);
            }

            resources.Add(new ResourceAsset(descriptor.Route, properties));
        }
        else
        {
            resources.Add(new ResourceAsset(descriptor.Route, null));
        }
    }

#if MVC_VIEWFEATURES
    internal ImportMapDefinition ResolveImportMap()
    {
        return _importMapDefinition ??= ImportMapDefinition.FromResourceCollection(_resourceCollection);
    }
#endif
}
