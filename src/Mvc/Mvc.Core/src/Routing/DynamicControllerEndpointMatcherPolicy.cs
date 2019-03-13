// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class DynamicControllerEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        private readonly DynamicControllerEndpointSelector _selector;

        public DynamicControllerEndpointMatcherPolicy(DynamicControllerEndpointSelector selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            _selector = selector;
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
                var metadata = endpoints[i].Metadata.GetMetadata<DynamicControllerMetadata>();
                if (metadata != null)
                {
                    // Found a dynamic controller endpoint
                    return true;
                }
            }

            return false;
        }

        public Task ApplyAsync(HttpContext httpContext, EndpointSelectorContext context, CandidateSet candidates)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
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

                var metadata = endpoint.Metadata.GetMetadata<DynamicControllerMetadata>();
                if (metadata == null)
                {
                    continue;
                }

                var matchedValues = candidates[i].Values;
                var endpoints = _selector.SelectEndpoints(metadata.Values);
                if (endpoints.Count == 0)
                {
                    // If there's no match this is a configuration error. We can't really check
                    // during startup that the action you configured exists.
                    throw new InvalidOperationException(
                        "Cannot find the fallback endpoint specified by route values: " + 
                        "{ " + string.Join(", ", metadata.Values.Select(kvp => $"{kvp.Key}: {kvp.Value}")) + " }.");
                }

                var replacement = endpoints[0];
                
                // We need to provide the route values associated with this endpoint, so that features
                // like URL generation work.
                var values = new RouteValueDictionary(metadata.Values);

                // Include values that were matched by the fallback route.
                foreach (var kvp in matchedValues)
                {
                    values.TryAdd(kvp.Key, kvp.Value);
                }

                candidates.ReplaceEndpoint(i, replacement, values);
            }

            return Task.CompletedTask;
        }
    }
}
