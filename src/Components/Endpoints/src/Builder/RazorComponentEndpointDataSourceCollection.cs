// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class RazorComponentEndpointDataSourceCollection : EndpointDataSource
{
    private readonly object _lock = new();
    private readonly List<EndpointDataSource> _dataSources = [];
    private CancellationTokenSource _cancellationTokenSource = new();

    public IReadOnlyList<EndpointDataSource> DataSources
    {
        get
        {
            lock (_lock)
            {
                return [.. _dataSources];
            }
        }
    }

    public void Add(EndpointDataSource dataSource)
    {
        CancellationTokenSource previousCancellationTokenSource;
        lock (_lock)
        {
            _dataSources.Add(dataSource);
            previousCancellationTokenSource = _cancellationTokenSource;
            _cancellationTokenSource = new();
        }

        previousCancellationTokenSource.Cancel();
        previousCancellationTokenSource.Dispose();
    }

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
                        endpoint.Metadata.GetMetadata<BlazorWebEndpointMetadata>() is not null &&
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
    {
        CancellationToken cancellationToken;
        IReadOnlyList<EndpointDataSource> dataSources;
        lock (_lock)
        {
            cancellationToken = _cancellationTokenSource.Token;
            dataSources = [.. _dataSources];
        }

        return new CompositeChangeToken([
            new CancellationChangeToken(cancellationToken),
            .. dataSources.Select(source => source.GetChangeToken()),
        ]);
    }

    private static bool AreEquivalent(RouteEndpoint first, RouteEndpoint second)
    {
        if (!AreRoutePatternsEquivalent(first.RoutePattern, second.RoutePattern) ||
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
            (HostAttribute left, HostAttribute right) => left.Hosts.SequenceEqual(right.Hosts),
            (AuthorizeAttribute left, AuthorizeAttribute right)
                when left.GetType() == typeof(AuthorizeAttribute) && right.GetType() == typeof(AuthorizeAttribute) =>
                left.Policy == right.Policy &&
                left.Roles == right.Roles &&
                left.AuthenticationSchemes == right.AuthenticationSchemes,
            (IRouteDiagnosticsMetadata left, IRouteDiagnosticsMetadata right)
                when left.GetType() == right.GetType() && left.GetType().Assembly == typeof(RouteEndpoint).Assembly =>
                left.Route == right.Route,
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

    private static bool AreRoutePatternsEquivalent(RoutePattern first, RoutePattern second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (!string.Equals(first.RawText, second.RawText, StringComparison.Ordinal) ||
            first.PathSegments.Count != second.PathSegments.Count ||
            !AreValuesEquivalent(first.Defaults, second.Defaults) ||
            !AreValuesEquivalent(first.RequiredValues, second.RequiredValues) ||
            !AreParameterPolicyMapsEquivalent(first.ParameterPolicies, second.ParameterPolicies) ||
            !AreParameterPartsEquivalent(first.Parameters, second.Parameters))
        {
            return false;
        }

        for (var i = 0; i < first.PathSegments.Count; i++)
        {
            var firstParts = first.PathSegments[i].Parts;
            var secondParts = second.PathSegments[i].Parts;
            if (firstParts.Count != secondParts.Count)
            {
                return false;
            }

            for (var j = 0; j < firstParts.Count; j++)
            {
                if (!AreRoutePatternPartsEquivalent(firstParts[j], secondParts[j]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool AreRoutePatternPartsEquivalent(RoutePatternPart first, RoutePatternPart second)
    {
        return (first, second) switch
        {
            (RoutePatternLiteralPart left, RoutePatternLiteralPart right) => left.Content == right.Content,
            (RoutePatternSeparatorPart left, RoutePatternSeparatorPart right) => left.Content == right.Content,
            (RoutePatternParameterPart left, RoutePatternParameterPart right) =>
                left.Name == right.Name &&
                left.ParameterKind == right.ParameterKind &&
                left.EncodeSlashes == right.EncodeSlashes &&
                Equals(left.Default, right.Default) &&
                AreParameterPoliciesEquivalent(left.ParameterPolicies, right.ParameterPolicies),
            _ => false,
        };
    }

    private static bool AreParameterPartsEquivalent(
        IReadOnlyList<RoutePatternParameterPart> first,
        IReadOnlyList<RoutePatternParameterPart> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (!AreRoutePatternPartsEquivalent(first[i], second[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreParameterPoliciesEquivalent(
        IReadOnlyList<RoutePatternParameterPolicyReference> first,
        IReadOnlyList<RoutePatternParameterPolicyReference> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (first[i].Content != second[i].Content ||
                !ReferenceEquals(first[i].ParameterPolicy, second[i].ParameterPolicy))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreParameterPolicyMapsEquivalent(
        IReadOnlyDictionary<string, IReadOnlyList<RoutePatternParameterPolicyReference>> first,
        IReadOnlyDictionary<string, IReadOnlyList<RoutePatternParameterPolicyReference>> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (var (key, policies) in first)
        {
            if (!second.TryGetValue(key, out var secondPolicies) ||
                !AreParameterPoliciesEquivalent(policies, secondPolicies))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreValuesEquivalent(IReadOnlyDictionary<string, object?> first, IReadOnlyDictionary<string, object?> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (var (key, value) in first)
        {
            if (!second.TryGetValue(key, out var secondValue) || !Equals(value, secondValue))
            {
                return false;
            }
        }

        return true;
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
