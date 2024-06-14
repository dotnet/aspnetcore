// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Builder;

internal class ResourceCollectionConvention(ResourceCollectionResolver resolver)
{
    private string? _collectionUrl;
    private ImportMapDefinition? _collectionEndpointImportMap;
    private ResourceAssetCollection? _collection;
    private ImportMapDefinition? _collectionImportMap;

    public void OnBeforeCreateEndpoints(RazorComponentEndpointUpdateContext context)
    {
        if (resolver.IsRegistered(context.Options.ManifestPath))
        {
            _collection = resolver.ResolveResourceCollection(context.Options.ManifestPath);
            _collectionImportMap = ImportMapDefinition.FromResourceCollection(_collection);

            string? url = null;
            ImportMapDefinition? map = null;
            foreach (var renderMode in context.Options.ConfiguredRenderModes)
            {
                if (renderMode is InteractiveWebAssemblyRenderMode or InteractiveAutoRenderMode)
                {
                    (map, url) = ResourceCollectionUrlEndpoint.MapResourceCollectionEndpoints(
                        context.Endpoints,
                        "_framework/resource-collection{0}.js{1}",
                        _collection);
                    break;
                }
            }

            if (url != null && map != null)
            {
                _collectionUrl = url;
                _collectionEndpointImportMap = map;
            }
        }
    }

    public void ApplyConvention(EndpointBuilder eb)
    {
        // The user called MapStaticAssets
        if (_collection != null && _collectionImportMap != null)
        {
            eb.Metadata.Add(_collection);

            if (_collectionUrl != null)
            {
                eb.Metadata.Add(new ResourceCollectionUrlMetadata(_collectionUrl));
            }

            var importMap = _collectionEndpointImportMap == null ? _collectionImportMap :
                ImportMapDefinition.Combine(_collectionImportMap, _collectionEndpointImportMap);
            eb.Metadata.Add(importMap);
        }
    }
}
