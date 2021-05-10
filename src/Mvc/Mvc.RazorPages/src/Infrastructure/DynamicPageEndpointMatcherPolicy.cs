// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DynamicPageEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        private readonly DynamicPageEndpointSelectorCache _selectorCache;
        private readonly PageLoader _loader;
        private readonly EndpointMetadataComparer _comparer;

        public DynamicPageEndpointMatcherPolicy(DynamicPageEndpointSelectorCache selectorCache, PageLoader loader, EndpointMetadataComparer comparer)
        {
            if (selectorCache == null)
            {
                throw new ArgumentNullException(nameof(selectorCache));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            _selectorCache = selectorCache;
            _loader = loader;
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
                // Dynamic page endpoints are always dynamic endpoints.
                return false;
            }

            for (var i = 0; i < endpoints.Count; i++)
            {
                if (endpoints[i].Metadata.GetMetadata<DynamicPageMetadata>() != null)
                {
                    // Found a dynamic page endpoint
                    return true;
                }

                if (endpoints[i].Metadata.GetMetadata<DynamicPageRouteValueTransformerMetadata>() != null)
                {
                    // Found a dynamic page endpoint
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

            DynamicPageEndpointSelector selector = null;

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
                var dynamicPageMetadata = endpoint.Metadata.GetMetadata<DynamicPageMetadata>();
                var transformerMetadata = endpoint.Metadata.GetMetadata<DynamicPageRouteValueTransformerMetadata>();
                DynamicRouteValueTransformer transformer = null;
                if (dynamicPageMetadata != null)
                {
                    dynamicValues = dynamicPageMetadata.Values;
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
                    // Not a dynamic page
                    continue;
                }

                if (dynamicValues == null)
                {
                    candidates.ReplaceEndpoint(i, null, null);
                    continue;
                }

                selector = ResolveSelector(selector, endpoint);
                var endpoints = selector.SelectEndpoints(dynamicValues);
                if (endpoints.Count == 0 && dynamicPageMetadata != null)
                {
                    // Having no match for a fallback is a configuration error. We can't really check
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

                var loadedEndpoints = new List<Endpoint>(endpoints);
                for (var j = 0; j < loadedEndpoints.Count; j++)
                {
                    var metadata = loadedEndpoints[j].Metadata;
                    var actionDescriptor = metadata.GetMetadata<PageActionDescriptor>();
                    if (actionDescriptor is CompiledPageActionDescriptor)
                    {
                        // Nothing to do here. The endpoint already represents a compiled page.
                    }
                    else
                    {
                        // We're working with a runtime-compiled page and have to Load it.
                        var compiled = actionDescriptor.CompiledPageDescriptor ??
                            await _loader.LoadAsync(actionDescriptor, endpoint.Metadata);
                        loadedEndpoints[j] = compiled.Endpoint;
                    }
                }

                // Expand the list of endpoints
                candidates.ExpandEndpoint(i, loadedEndpoints, _comparer);
            }
        }

        private DynamicPageEndpointSelector ResolveSelector(DynamicPageEndpointSelector currentSelector, Endpoint endpoint)
        {
            var selector = _selectorCache.GetEndpointSelector(endpoint);

            Debug.Assert(currentSelector == null || ReferenceEquals(currentSelector, selector));

            return selector;
        }

    }
}
