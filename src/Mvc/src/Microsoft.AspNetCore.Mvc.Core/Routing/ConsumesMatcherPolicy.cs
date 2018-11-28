// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ConsumesMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
    {
        internal const string Http415EndpointDisplayName = "415 HTTP Unsupported Media Type";
        internal const string AnyContentType = "*/*";

        // Run after HTTP methods, but before 'default'.
        public override int Order { get; } = -100;

        public IComparer<Endpoint> Comparer { get; } = new ConsumesMetadataEndpointComparer();

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            return endpoints.Any(e => e.Metadata.GetMetadata<IConsumesMetadata>()?.ContentTypes.Count > 0);
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            // The algorithm here is designed to be preserve the order of the endpoints
            // while also being relatively simple. Preserving order is important.

            // First, build a dictionary of all of the content-type patterns that are included
            // at this node.
            //
            // For now we're just building up the set of keys. We don't add any endpoints
            // to lists now because we don't want ordering problems.
            var edges = new Dictionary<string, List<Endpoint>>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                var contentTypes = endpoint.Metadata.GetMetadata<IConsumesMetadata>()?.ContentTypes;
                if (contentTypes == null || contentTypes.Count == 0)
                {
                    contentTypes = new string[] { AnyContentType, };
                }

                for (var j = 0; j < contentTypes.Count; j++)
                {
                    var contentType = contentTypes[j];

                    if (!edges.ContainsKey(contentType))
                    {
                        edges.Add(contentType, new List<Endpoint>());
                    }
                }
            }

            // Now in a second loop, add endpoints to these lists. We've enumerated all of
            // the states, so we want to see which states this endpoint matches.
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                var contentTypes = endpoint.Metadata.GetMetadata<IConsumesMetadata>()?.ContentTypes ?? Array.Empty<string>();
                if (contentTypes.Count == 0)
                {
                    // OK this means that this endpoint matches *all* content methods.
                    // So, loop and add it to all states.
                    foreach (var kvp in edges)
                    {
                        kvp.Value.Add(endpoint);
                    }
                }
                else
                {
                    // OK this endpoint matches specific content types -- we have to loop through edges here
                    // because content types could either be exact (like 'application/json') or they
                    // could have wildcards (like 'text/*'). We don't expect wildcards to be especially common
                    // with consumes, but we need to support it.
                    foreach (var kvp in edges)
                    {
                        // The edgeKey maps to a possible request header value
                        var edgeKey = new MediaType(kvp.Key);

                        for (var j = 0; j < contentTypes.Count; j++)
                        {
                            var contentType = contentTypes[j];

                            var mediaType = new MediaType(contentType);

                            // Example: 'application/json' is subset of 'application/*'
                            // 
                            // This means that when the request has content-type 'application/json' an endpoint
                            // what consumes 'application/*' should match.
                            if (edgeKey.IsSubsetOf(mediaType))
                            {
                                kvp.Value.Add(endpoint);

                                // It's possible that a ConsumesMetadata defines overlapping wildcards. Don't add an endpoint
                                // to any edge twice
                                break;
                            }
                        }
                    }
                }
            }

            // If after we're done there isn't any endpoint that accepts */*, then we'll synthesize an
            // endpoint that always returns a 415.
            if (!edges.ContainsKey(AnyContentType))
            {
                edges.Add(AnyContentType, new List<Endpoint>()
                {
                    CreateRejectionEndpoint(),
                });
            }

            return edges
                .Select(kvp => new PolicyNodeEdge(kvp.Key, kvp.Value))
                .ToArray();
        }

        private Endpoint CreateRejectionEndpoint()
        {
            return new Endpoint(
                (context) =>
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return Task.CompletedTask;
                },
                EndpointMetadataCollection.Empty,
                Http415EndpointDisplayName);
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(nameof(edges));
            }

            // Since our 'edges' can have wildcards, we do a sort based on how wildcard-ey they
            // are then then execute them in linear order.
            var ordered = edges
                .Select(e => (mediaType: new MediaType((string)e.State), destination: e.Destination))
                .OrderBy(e => GetScore(e.mediaType))
                .ToArray();

            // If any edge matches all content types, then treat that as the 'exit'. This will
            // always happen because we insert a 415 endpoint.
            for (var i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].mediaType.MatchesAllTypes)
                {
                    exitDestination = ordered[i].destination;
                    break;
                }
            }

            return new ConsumesPolicyJumpTable(exitDestination, ordered);
        }

        private int GetScore(in MediaType mediaType)
        {
            // Higher score == lower priority - see comments on MediaType.
            if (mediaType.MatchesAllTypes)
            {
                return 4;
            }
            else if (mediaType.MatchesAllSubTypes)
            {
                return 3;
            }
            else if (mediaType.MatchesAllSubTypesWithoutSuffix)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        private class ConsumesMetadataEndpointComparer : EndpointMetadataComparer<IConsumesMetadata>
        {
            protected override int CompareMetadata(IConsumesMetadata x, IConsumesMetadata y)
            {
                // Ignore the metadata if it has an empty list of content types.
                return base.CompareMetadata(
                    x?.ContentTypes.Count > 0 ? x : null,
                    y?.ContentTypes.Count > 0 ? y : null);
            }
        }

        private class ConsumesPolicyJumpTable : PolicyJumpTable
        {
            private (MediaType mediaType, int destination)[] _destinations;
            private int _exitDestination;

            public ConsumesPolicyJumpTable(int exitDestination, (MediaType mediaType, int destination)[] destinations)
            {
                _exitDestination = exitDestination;
                _destinations = destinations;
            }

            public override int GetDestination(HttpContext httpContext)
            {
                var contentType = httpContext.Request.ContentType;
                if (string.IsNullOrEmpty(contentType))
                {
                    return _exitDestination;
                }

                var requestMediaType = new MediaType(contentType);
                var destinations = _destinations;
                for (var i = 0; i < destinations.Length; i++)
                {
                    if (requestMediaType.IsSubsetOf(destinations[i].mediaType))
                    {
                        return destinations[i].destination;
                    }
                }

                return _exitDestination;
            }
        }
    }
}
