// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class DynamicControllerEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        private readonly DynamicControllerEndpointSelector _selector;
        private readonly EndpointMetadataComparer _comparer;

        public DynamicControllerEndpointMatcherPolicy(DynamicControllerEndpointSelector selector, EndpointMetadataComparer comparer)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            _selector = selector;
            _comparer = comparer;
        }

        public override int Order => int.MinValue + 100;

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

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
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

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
                var originalValues = candidates[i].Values;

                RouteValueDictionary dynamicValues = null;

                // We don't expect both of these to be provided, and they are internal so there's
                // no realistic way this could happen.
                var dynamicControllerMetadata = endpoint.Metadata.GetMetadata<DynamicControllerMetadata>();
                var transformerMetadata = endpoint.Metadata.GetMetadata<DynamicControllerRouteValueTransformerMetadata>();
                if (dynamicControllerMetadata != null)
                {
                    dynamicValues = dynamicControllerMetadata.Values;
                }
                else if (transformerMetadata != null)
                {
                    var transformer = (DynamicRouteValueTransformer)httpContext.RequestServices.GetRequiredService(transformerMetadata.SelectorType);
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

                var endpoints = _selector.SelectEndpoints(dynamicValues);
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

                // Update the route values
                candidates.ReplaceEndpoint(i, endpoint, values);

                // Expand the list of endpoints
                candidates.ExpandEndpoint(i, endpoints, _comparer);
            }
        }
    }
}
