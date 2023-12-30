// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class DynamicControllerEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    private readonly DynamicControllerEndpointSelectorCache _selectorCache;
    private readonly EndpointMetadataComparer _comparer;

    public DynamicControllerEndpointMatcherPolicy(DynamicControllerEndpointSelectorCache selectorCache, EndpointMetadataComparer comparer)
    {
        ArgumentNullException.ThrowIfNull(selectorCache);
        ArgumentNullException.ThrowIfNull(comparer);

        _selectorCache = selectorCache;
        _comparer = comparer;
    }

    public override int Order => int.MinValue + 100;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (!ContainsDynamicEndpoints(endpoints))
        {
            // Dynamic controller endpoints are always dynamic endpoints.
            return false;
        }

        for (var i = 0; i < endpoints.Count; i++)
        {
            if (endpoints[i].Metadata.GetMetadata<DynamicControllerMetadata>() != null)
            {
                // Found a dynamic controller endpoint
                return true;
            }

            if (endpoints[i].Metadata.GetMetadata<DynamicControllerRouteValueTransformerMetadata>() != null)
            {
                // Found a dynamic controller endpoint
                return true;
            }
        }

        return false;
    }

    public async Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(candidates);

        // The per-route selector, must be the same for all the endpoints we are dealing with.
        DynamicControllerEndpointSelector? selector = null;

        // There's no real benefit here from trying to avoid the async state machine.
        // We only execute on nodes that contain a dynamic policy, and thus always have
        // to await something.
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var endpoint = candidates[i].Endpoint;
            var originalValues = candidates[i].Values!;

            RouteValueDictionary? dynamicValues = null;

            // We don't expect both of these to be provided, and they are internal so there's
            // no realistic way this could happen.
            var dynamicControllerMetadata = endpoint.Metadata.GetMetadata<DynamicControllerMetadata>();
            var transformerMetadata = endpoint.Metadata.GetMetadata<DynamicControllerRouteValueTransformerMetadata>();

            DynamicRouteValueTransformer? transformer = null;
            if (dynamicControllerMetadata != null)
            {
                dynamicValues = dynamicControllerMetadata.Values;
            }
            else if (transformerMetadata != null)
            {
                transformer = (DynamicRouteValueTransformer)httpContext.RequestServices.GetRequiredService(transformerMetadata.SelectorType);
                if (transformer.State != null)
                {
                    throw new InvalidOperationException(Resources.FormatStateShouldBeNullForRouteValueTransformers(transformerMetadata.SelectorType.Name));
                }
                transformer.State = transformerMetadata.State;

                dynamicValues = await transformer.TransformAsync(httpContext, originalValues);
            }
            else
            {
                // Not a dynamic controller.
                continue;
            }

            if (dynamicValues == null)
            {
                candidates.ReplaceEndpoint(i, null, null);
                continue;
            }

            selector = ResolveSelector(selector, endpoint);

            var endpoints = selector.SelectEndpoints(dynamicValues);
            if (endpoints.Count == 0 && dynamicControllerMetadata != null)
            {
                // Naving no match for a fallback is a configuration error. We can't really check
                // during startup that the action you configured exists, so this is the best we can do.
                throw new InvalidOperationException(
                    "Cannot find the fallback endpoint specified by route values: " +
                    "{ " + string.Join(", ", dynamicValues.Select(kvp => $"{kvp.Key}: {kvp.Value}")) + " }.");
            }
            else if (endpoints.Count == 0)
            {
                candidates.ReplaceEndpoint(i, null, null);
                continue;
            }

            // We need to provide the route values associated with this endpoint, so that features
            // like URL generation work.
            var values = new RouteValueDictionary(dynamicValues);

            // Include values that were matched by the fallback route.
            if (originalValues != null)
            {
                foreach (var kvp in originalValues)
                {
                    values.TryAdd(kvp.Key, kvp.Value);
                }
            }

            if (transformer != null)
            {
                endpoints = await transformer.FilterAsync(httpContext, values, endpoints);
                if (endpoints.Count == 0)
                {
                    candidates.ReplaceEndpoint(i, null, null);
                    continue;
                }
            }

            // Update the route values
            candidates.ReplaceEndpoint(i, endpoint, values);

            // Expand the list of endpoints
            candidates.ExpandEndpoint(i, endpoints, _comparer);
        }
    }

    private DynamicControllerEndpointSelector ResolveSelector(DynamicControllerEndpointSelector? currentSelector, Endpoint endpoint)
    {
        var selector = _selectorCache.GetEndpointSelector(endpoint);

        Debug.Assert(currentSelector == null || ReferenceEquals(currentSelector, selector));

        return selector;
    }
}
