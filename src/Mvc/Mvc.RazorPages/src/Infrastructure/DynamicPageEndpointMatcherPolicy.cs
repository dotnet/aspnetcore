// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DynamicPageEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        private readonly DynamicPageEndpointSelector _selector;
        private readonly PageLoader _loader;

        public DynamicPageEndpointMatcherPolicy(DynamicPageEndpointSelector selector, PageLoader loader)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            _selector = selector;
            _loader = loader;
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
                // Dynamic page endpoints are always dynamic endpoints.
                return false;
            }

            for (var i = 0; i < endpoints.Count; i++)
            {
                var metadata = endpoints[i].Metadata.GetMetadata<DynamicPageMetadata>();
                if (metadata != null)
                {
                    // Found a dynamic page endpoint
                    return true;
                }
            }

            return false;
        }

        public async Task ApplyAsync(HttpContext httpContext, EndpointSelectorContext context, CandidateSet candidates)
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

                var metadata = endpoint.Metadata.GetMetadata<DynamicPageMetadata>();
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

                var compiled = await _loader.LoadAsync(endpoints[0].Metadata.GetMetadata<PageActionDescriptor>());
                var replacement = compiled.Endpoint;
                
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
        }
    }
}
