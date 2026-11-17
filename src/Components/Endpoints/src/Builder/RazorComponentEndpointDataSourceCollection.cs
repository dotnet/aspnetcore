// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class RazorComponentEndpointDataSourceCollection : EndpointDataSource
{
    public List<EndpointDataSource> DataSources { get; } = [];

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            var endpoints = new List<Endpoint>();
            foreach (var source in DataSources)
            {
                foreach (var endpoint in source.Endpoints)
                {
                    if (endpoint is RouteEndpoint routeEndpoint &&
                        endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is { } metadata &&
                        ReferenceEquals(routeEndpoint.RoutePattern, metadata.RoutePattern) &&
                        endpoints.OfType<RouteEndpoint>().Any(existing => AreEquivalent(existing, routeEndpoint)))
                    {
                        continue;
                    }

                    endpoints.Add(endpoint);
                }
            }

            return endpoints;
        }
    }

    public override IChangeToken GetChangeToken()
        => new CompositeChangeToken(DataSources.Select(source => source.GetChangeToken()).ToArray());

    private static bool AreEquivalent(RouteEndpoint first, RouteEndpoint second)
    {
        if (!ReferenceEquals(first.RoutePattern, second.RoutePattern) ||
            first.RequestDelegate != second.RequestDelegate ||
            first.Order != second.Order ||
            first.DisplayName != second.DisplayName ||
            first.Metadata.Count != second.Metadata.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Metadata.Count; i++)
        {
            var firstMetadata = first.Metadata[i];
            var secondMetadata = second.Metadata[i];
            if (AreMetadataEquivalent(firstMetadata, secondMetadata))
            {
                continue;
            }

            // Unknown metadata can carry root-specific behavior. Do not select one root's conventions.
            return false;
        }

        return true;
    }

    private static bool AreMetadataEquivalent(object first, object second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        return (first, second) switch
        {
            (HttpMethodMetadata left, HttpMethodMetadata right) =>
                left.AcceptCorsPreflight == right.AcceptCorsPreflight && left.HttpMethods.SequenceEqual(right.HttpMethods),
            (ResourceAssetCollection left, ResourceAssetCollection right) => AreAssetsEquivalent(left, right),
            (ImportMapDefinition left, ImportMapDefinition right) => left.ToJson() == right.ToJson(),
            (ResourcePreloadCollection left, ResourcePreloadCollection right)
                when left.GetType() == typeof(ResourcePreloadCollection) && right.GetType() == typeof(ResourcePreloadCollection) =>
                left.HasSameAssets(right),
            (ResourceCollectionUrlMetadata left, ResourceCollectionUrlMetadata right)
                when left.GetType() == typeof(ResourceCollectionUrlMetadata) && right.GetType() == typeof(ResourceCollectionUrlMetadata) =>
                left.Url == right.Url,
            _ => false,
        };
    }

    private static bool AreAssetsEquivalent(IReadOnlyList<ResourceAsset> first, IReadOnlyList<ResourceAsset> second)
    {
        return first.Count == second.Count && first.Zip(second).All(pair =>
            pair.First.Url == pair.Second.Url &&
            (ReferenceEquals(pair.First.Properties, pair.Second.Properties) ||
                pair.First.Properties is { } firstProperties && pair.Second.Properties is { } secondProperties &&
                firstProperties.Count == secondProperties.Count &&
                firstProperties.Zip(secondProperties).All(property =>
                    property.First.Name == property.Second.Name && property.First.Value == property.Second.Value)));
    }
}
