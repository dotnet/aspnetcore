// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class RazorComponentsEndpointDataSource : EndpointDataSource
{
    internal List<EndpointDataSource> DataSources { get; } = [];

    internal bool IsInitialized { get; private set; }

    public override IReadOnlyList<Endpoint> Endpoints => Deduplicate(Initialize().SelectMany(source => source.Endpoints).ToArray());

    public override IChangeToken GetChangeToken() => new CompositeChangeToken(Initialize().Select(source => source.GetChangeToken()).ToArray());

    public override IReadOnlyList<Endpoint> GetGroupedEndpoints(RouteGroupContext context)
        => Deduplicate(Initialize().SelectMany(source => source.GetGroupedEndpoints(context)).ToArray());

    private List<EndpointDataSource> Initialize()
    {
        IsInitialized = true;

        return DataSources;
    }

    private static IReadOnlyList<Endpoint> Deduplicate(IReadOnlyList<Endpoint> endpoints)
    {
        var result = new List<Endpoint>(endpoints.Count);
        var helpers = new List<RouteEndpoint>();
        foreach (var endpoint in endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint &&
                endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is { } marker &&
                endpoint.RequestDelegate == marker.RequestDelegate)
            {
                if (helpers.Any(other => Equivalent(routeEndpoint, other)))
                {
                    continue;
                }

                helpers.Add(routeEndpoint);
            }

            result.Add(endpoint);
        }

        return result;
    }

    private static bool Equivalent(RouteEndpoint endpoint, RouteEndpoint other)
    {
        if (endpoint.RequestDelegate != other.RequestDelegate ||
            endpoint.Order != other.Order ||
            endpoint.DisplayName != other.DisplayName ||
            !EquivalentPatterns(endpoint.RoutePattern, other.RoutePattern) ||
            endpoint.Metadata.Count != other.Metadata.Count)
        {
            return false;
        }

        var marker = endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>()!;
        var otherMarker = other.Metadata.GetMetadata<BlazorWebEndpointMetadata>()!;
        for (var i = 0; i < endpoint.Metadata.Count; i++)
        {
            var left = endpoint.Metadata[i];
            var right = other.Metadata[i];
            if (!ReferenceEquals(left, right) && !((left, right) switch
            {
                (BlazorWebEndpointMetadata a, BlazorWebEndpointMetadata b) => a.RequestDelegate == b.RequestDelegate,
                (HttpMethodMetadata a, HttpMethodMetadata b) => a.AcceptCorsPreflight == b.AcceptCorsPreflight && a.HttpMethods.SequenceEqual(b.HttpMethods, StringComparer.Ordinal),
                _ => (ReferenceEquals(left, marker.GeneratedDiagnosticsMetadata) && ReferenceEquals(right, otherMarker.GeneratedDiagnosticsMetadata) &&
                    marker.GeneratedDiagnosticsMetadata!.Route == otherMarker.GeneratedDiagnosticsMetadata!.Route) ||
                    (marker.GeneratedResourceMetadata.Contains(left) && otherMarker.GeneratedResourceMetadata.Contains(right) && EquivalentResources(left, right)),
            }))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EquivalentResources(object left, object right) => (left, right) switch
    {
        (ResourceAssetCollection a, ResourceAssetCollection b) => EquivalentAssets(a, b),
        (ResourcePreloadCollection a, ResourcePreloadCollection b) => EquivalentAssets(a.Assets, b.Assets),
        (ResourceCollectionUrlMetadata a, ResourceCollectionUrlMetadata b) => a.Url == b.Url,
        (ImportMapDefinition a, ImportMapDefinition b) => a.ToJson() == b.ToJson(),
        _ => false,
    };

    private static bool EquivalentAssets(IReadOnlyList<ResourceAsset> left, IReadOnlyList<ResourceAsset> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            var a = left[i];
            var b = right[i];
            if (a.Url != b.Url || !(ReferenceEquals(a.Properties, b.Properties) ||
                (a.Properties is { } properties && b.Properties is { } otherProperties &&
                properties.Count == otherProperties.Count && properties.Zip(otherProperties).All(pair =>
                    pair.First.Name == pair.Second.Name && pair.First.Value == pair.Second.Value))))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EquivalentDictionaries<T>(IReadOnlyDictionary<string, T> left, IReadOnlyDictionary<string, T> right, Func<T, T, bool> equivalentValue)
        => ReferenceEquals(left, right) || (left.Count == right.Count && left.All(entry =>
            right.Any(other => entry.Key == other.Key && equivalentValue(entry.Value, other.Value))));

    private static bool EquivalentPatterns(RoutePattern left, RoutePattern right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        // Shared group prefixes retain their parts and values, but combining patterns can copy
        // dictionaries and policy lists. Reparsed parameters and application values stay distinct.
        if (left.RawText != right.RawText ||
            !EquivalentDictionaries(left.Defaults, right.Defaults, static (a, b) => ReferenceEquals(a, b)) ||
            !EquivalentDictionaries(left.RequiredValues, right.RequiredValues, static (a, b) => ReferenceEquals(a, b)) ||
            !EquivalentDictionaries(left.ParameterPolicies, right.ParameterPolicies, static (a, b) =>
                ReferenceEquals(a, b) || (a.Count == b.Count && a.Zip(b).All(pair => ReferenceEquals(pair.First, pair.Second)))) ||
            left.PathSegments.Count != right.PathSegments.Count)
        {
            return false;
        }

        for (var i = 0; i < left.PathSegments.Count; i++)
        {
            var leftParts = left.PathSegments[i].Parts;
            var rightParts = right.PathSegments[i].Parts;
            if (leftParts.Count != rightParts.Count)
            {
                return false;
            }

            for (var j = 0; j < leftParts.Count; j++)
            {
                if (!ReferenceEquals(leftParts[j], rightParts[j]) &&
                    !(leftParts[j] is RoutePatternLiteralPart a && rightParts[j] is RoutePatternLiteralPart b && a.Content == b.Content))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
