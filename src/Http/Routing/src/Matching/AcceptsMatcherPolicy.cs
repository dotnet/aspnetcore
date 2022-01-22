// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class AcceptsMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy, IEndpointSelectorPolicy
{
    internal const string Http415EndpointDisplayName = "415 HTTP Unsupported Media Type";
    internal const string AnyContentType = "*/*";

    // Run after HTTP methods, but before 'default'.
    public override int Order { get; } = -100;

    public IComparer<Endpoint> Comparer { get; } = new ConsumesMetadataEndpointComparer();

    bool INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        if (ContainsDynamicEndpoints(endpoints))
        {
            return false;
        }

        return AppliesToEndpointsCore(endpoints);
    }

    bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        // When the node contains dynamic endpoints we can't make any assumptions.
        return ContainsDynamicEndpoints(endpoints);
    }

    private static bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
    {
        return endpoints.Any(e => e.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes.Count > 0);
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (candidates == null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        // We want to return a 415 if we eliminated ALL of the currently valid endpoints due to content type
        // mismatch.
        bool? needs415Endpoint = null;

        for (var i = 0; i < candidates.Count; i++)
        {
            // We do this check first for consistency with how 415 is implemented for the graph version
            // of this code. We still want to know if any endpoints in this set require an a ContentType
            // even if those endpoints are already invalid - hence the null check.
            var metadata = candidates[i].Endpoint?.Metadata.GetMetadata<IAcceptsMetadata>();
            if (metadata == null || metadata.ContentTypes?.Count == 0)
            {
                // Can match any content type.
                needs415Endpoint = false;
                continue;
            }

            // Saw a valid endpoint.
            needs415Endpoint = needs415Endpoint ?? true;

            if (!candidates.IsValidCandidate(i))
            {
                // If the candidate is already invalid, then do a search to see if it has a wildcard content type.
                //
                // We don't want to return a 415 if any content type could be accepted depending on other parameters.
                if (metadata != null)
                {
                    for (var j = 0; j < metadata.ContentTypes?.Count; j++)
                    {
                        if (string.Equals("*/*", metadata.ContentTypes[j], StringComparison.Ordinal))
                        {
                            needs415Endpoint = false;
                            break;
                        }
                    }
                }

                continue;
            }

            var contentType = httpContext.Request.ContentType;
            var mediaType = string.IsNullOrEmpty(contentType) ? (ReadOnlyMediaTypeHeaderValue?)null : new(contentType);

            var matched = false;
            for (var j = 0; j < metadata.ContentTypes?.Count; j++)
            {
                var candidateMediaType = new ReadOnlyMediaTypeHeaderValue(metadata.ContentTypes[j]);
                if (candidateMediaType.MatchesAllTypes)
                {
                    // We don't need a 415 response because there's an endpoint that would accept any type.
                    needs415Endpoint = false;
                }

                // If there's no ContentType, then then can only matched by a wildcard `*/*`.
                if (mediaType == null && !candidateMediaType.MatchesAllTypes)
                {
                    continue;
                }

                // We have a ContentType but it's not a match.
                else if (mediaType != null && !mediaType.Value.IsSubsetOf(candidateMediaType))
                {
                    continue;
                }

                // We have a ContentType and we accept any value OR we have a ContentType and it's a match.
                matched = true;
                needs415Endpoint = false;
                break;
            }

            if (!matched)
            {
                candidates.SetValidity(i, false);
            }
        }

        if (needs415Endpoint == true)
        {
            // We saw some endpoints coming in, and we eliminated them all.
            httpContext.SetEndpoint(CreateRejectionEndpoint());
        }

        return Task.CompletedTask;
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
            var contentTypes = endpoint.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes;
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
            var contentTypes = endpoint.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes ?? Array.Empty<string>();
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
                    var edgeKey = new ReadOnlyMediaTypeHeaderValue(kvp.Key);

                    for (var j = 0; j < contentTypes.Count; j++)
                    {
                        var contentType = contentTypes[j];

                        var mediaType = new ReadOnlyMediaTypeHeaderValue(contentType);

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
        if (!edges.TryGetValue(AnyContentType, out var anyEndpoints))
        {
            edges.Add(AnyContentType, new List<Endpoint>()
                {
                    CreateRejectionEndpoint(),
                });

            // Add a node to use when there is no request content type.
            // When there is no content type we want the policy to no-op
            edges.Add(string.Empty, endpoints.ToList());
        }
        else
        {
            // If there is an endpoint that accepts */* then it is also used when there is no content type
            edges.Add(string.Empty, anyEndpoints.ToList());
        }


        return edges
            .Select(kvp => new PolicyNodeEdge(kvp.Key, kvp.Value))
            .ToArray();
    }

    private static Endpoint CreateRejectionEndpoint()
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
            .Select(e => (mediaType: CreateEdgeMediaType(ref e), destination: e.Destination))
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

        var noContentTypeDestination = GetNoContentTypeDestination(ordered);

        return new ConsumesPolicyJumpTable(exitDestination, noContentTypeDestination, ordered);
    }

    private static int GetNoContentTypeDestination((ReadOnlyMediaTypeHeaderValue mediaType, int destination)[] destinations)
    {
        for (var i = 0; i < destinations.Length; i++)
        {
            var mediaType = destinations[i].mediaType;

            if (!mediaType.Type.HasValue)
            {
                return destinations[i].destination;
            }
        }

        throw new InvalidOperationException("Could not find destination for no content type.");
    }

    private static ReadOnlyMediaTypeHeaderValue CreateEdgeMediaType(ref PolicyJumpTableEdge e)
    {
        var mediaType = (string)e.State;
        return !string.IsNullOrEmpty(mediaType) ? new ReadOnlyMediaTypeHeaderValue(mediaType) : default;
    }

    private static int GetScore(ReadOnlyMediaTypeHeaderValue mediaType)
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

    private sealed class ConsumesMetadataEndpointComparer : EndpointMetadataComparer<IAcceptsMetadata>
    {
        protected override int CompareMetadata(IAcceptsMetadata? x, IAcceptsMetadata? y)
        {
            // Ignore the metadata if it has an empty list of content types.
            return base.CompareMetadata(
                x?.ContentTypes.Count > 0 ? x : null,
                y?.ContentTypes.Count > 0 ? y : null);
        }
    }

    private sealed class ConsumesPolicyJumpTable : PolicyJumpTable
    {
        private readonly (ReadOnlyMediaTypeHeaderValue mediaType, int destination)[] _destinations;
        private readonly int _exitDestination;
        private readonly int _noContentTypeDestination;

        public ConsumesPolicyJumpTable(int exitDestination, int noContentTypeDestination, (ReadOnlyMediaTypeHeaderValue mediaType, int destination)[] destinations)
        {
            _exitDestination = exitDestination;
            _noContentTypeDestination = noContentTypeDestination;
            _destinations = destinations;
        }

        public override int GetDestination(HttpContext httpContext)
        {
            var contentType = httpContext.Request.ContentType;

            if (string.IsNullOrEmpty(contentType))
            {
                return _noContentTypeDestination;
            }

            var requestMediaType = new ReadOnlyMediaTypeHeaderValue(contentType);
            var destinations = _destinations;
            for (var i = 0; i < destinations.Length; i++)
            {
                var destination = destinations[i].mediaType;
                if (requestMediaType.IsSubsetOf(destination))
                {
                    return destinations[i].destination;
                }
            }

            return _exitDestination;
        }
    }
}
