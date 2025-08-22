// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Routing.Matching;

internal abstract class NegotiationMatcherPolicy<TNegotiateMetadata> : MatcherPolicy, IEndpointSelectorPolicy, INodeBuilderPolicy, IEndpointComparerPolicy
    where TNegotiateMetadata : class, INegotiateMetadata
{
    private const string DefaultNegotiationValue = "identity";
    private static Endpoint? Http406Endpoint;
    internal const string Http406EndpointDisplayName = "406 HTTP Unsupported Encoding";

    // This policy runs very late in the pipeline, this ensures that any endpoint that might be potentially invalid
    // for other reasons, gets removed before we perform content negotiation.
    public override int Order => 10_000;

    public IComparer<Endpoint> Comparer => new NegotiationMetadataEndpointComparer();

    bool INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) =>
        !ContainsDynamicEndpoints(endpoints) && AppliesToEndpointsCore(endpoints);

    bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) =>
        ContainsDynamicEndpoints(endpoints) || AppliesToEndpointsCore(endpoints);

    private bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
    {
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            if (HasMetadata(endpoint))
            {
                return true;
            }
        }

        return false;
    }

    // Returns whether the endpoint has the metadata required for this policy to apply.
    private protected abstract bool HasMetadata(Endpoint endpoint);

    private protected abstract string? GetMetadataValue(Endpoint endpoint);

    private protected abstract StringValues GetNegotiationHeader(HttpContext httpContext);

    private protected abstract bool IsDefaultMetadataValue(ReadOnlySpan<char> candidate);

    private protected abstract double? GetMetadataQuality(Endpoint endpoint);

    // We iterate over the list of candidates starting with a quality of 0.
    // If we are able to match a candidate with one of the values from the header
    // the one with the highest matching quality wins.
    // It is ok for multiple candidates to tie, there is an ordering process based on
    // endpoint metadata that will break the tie.
    // It's also possible that none of the candidates can satisfy the header. Candidates without
    // metadata are always valid, but they have less priority that candidates with the metadata.
    // (They are considered less specific matches)
    // Algorithm mechanics
    // We iterate from 0 to N. At each point we try to match each header value with the value
    // from the endpoint.
    // The first time we find a match, that becomes the initial selection, at that point, we can
    // invalidate all the previous candidates, since they either didn't match or were defaults.
    // It is important to note that we receive the list of candidates already ordered by their specificity
    // which helps us simplify the algorithm.
    // From that point on, we continue iterating over the remaining candidates:
    // * If a candidate matches with a lower quality we invalidate it (we already have a better match).
    // * If a candidate matches with the same quality we keep it (we break the tie later on based on order,
    // or another policy might invalidate our current best match or another one).
    // * If a candidate matches with higher quality then we mark it as our best match and we invalidate
    // all the elements from the current element to the new best match.
    // After we've processed all candidates two things can happen:
    // * We found a compatible candidate -> We can return, all candidates with lower quality (or defaults) are invalidated.
    // * We haven't found a compatible candidate:
    //   * The default value was explicitly listed in the header -> No compatible candidate. Invalidate all.
    //   * The default value was not explicitly listed in the header -> Do nothing, we've invalidated already any endpoint with a non-default value for the metadata.
    //     * In this situation the most likely outcome is that there is a single remaining valid endpoint. For example, the uncompressed asset. Its even possible that there
    //       is more than one, and that's legitimate. We might be using a different policy to choose which endpoint is preferred.
    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        var header = GetNegotiationHeader(httpContext);
        if (StringValues.IsNullOrEmpty(header) ||
            !StringWithQualityHeaderValue.TryParseList(header, out var values) || values.Count == 0)
        {
            values = Array.Empty<StringWithQualityHeaderValue>();
        }

        // The candidates are already sorted based on the metadata quality for endpoints that contain the metadata
        // and endpoints with metadata are considered before (and preferred to) those without it.
        var sawCandidateWithoutMetadata = false;
        var sawValidCandidate = false;
        var bestMatchIndex = -1;
        var bestQualitySoFar = 0.0;
        var bestEndpointQualitySoFar = 0.0;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                // Skip invalid candidates.
                continue;
            }

            sawValidCandidate = true;

            ref var candidate = ref candidates[i];
            var metadata = GetMetadataValue(candidate.Endpoint);
            if (metadata is null)
            {
                sawCandidateWithoutMetadata = true;
            }
            metadata ??= DefaultNegotiationValue;

            var found = false;
            for (var j = 0; j < values.Count; j++)
            {
                var value = values[j];
                if (MemoryExtensions.Equals(metadata.AsSpan(), value.Value.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    EvaluateCandidate(candidates, ref bestMatchIndex, ref bestQualitySoFar, ref bestEndpointQualitySoFar, i, value);
                    break;
                }
            }

            if (!found && (bestMatchIndex >= 0 || metadata != DefaultNegotiationValue))
            {
                // We already have at least a candidate, and the default value was not part of the header, so we won't be considering it
                // at a later stage as a fallback.
                candidates.SetValidity(i, false);
            }
        }

        if (bestMatchIndex < 0 && !sawCandidateWithoutMetadata && sawValidCandidate)
        {
            // We did not see any candidate that matched the header and we did not see an endpoint
            // without the metadata.
            httpContext.SetEndpoint(CreateRejectionEndpoint());
            httpContext.Request.RouteValues = null!;
        }

        return Task.CompletedTask;
    }

    private void EvaluateCandidate(
        CandidateSet candidates,
        ref int bestMatchIndex,
        ref double bestQualitySoFar,
        ref double bestEndpointQualitySoFar,
        int currentIndex,
        StringWithQualityHeaderValue value)
    {
        var quality = value.Quality ?? 1.0;
        if ((quality - bestQualitySoFar) > double.Epsilon)
        {
            // The quality defined for this value is higher.
            bestQualitySoFar = quality;
            bestMatchIndex = Math.Max(bestMatchIndex, 0);
            bestEndpointQualitySoFar = GetMetadataQuality(candidates[currentIndex].Endpoint) ?? 1.0;

            // Since we found a better match, we can invalidate all the candidates from the current position to the new one.
            for (var j = bestMatchIndex; j < currentIndex; j++)
            {
                candidates.SetValidity(j, false);
            }

            bestMatchIndex = currentIndex;
        }
        else if ((bestQualitySoFar - quality) > double.Epsilon)
        {
            // The quality defined for this value is lower than the quality for the element we've selected so far.
            candidates.SetValidity(currentIndex, false);
        }
        else
        {
            // Header quality is equal to the best quality so far.
            // Evauate the quality of the metadata to break the tie.
            var endpointQuality = GetMetadataQuality(candidates[currentIndex].Endpoint) ?? 1.0;
            if ((endpointQuality - bestEndpointQualitySoFar) > double.Epsilon)
            {
                // Since we found a better match, we can invalidate all the candidates from the current position to the new one.
                for (var j = bestMatchIndex; j < currentIndex; j++)
                {
                    candidates.SetValidity(j, false);
                }
                // The quality defined for this value is higher.
                bestEndpointQualitySoFar = endpointQuality;
                bestMatchIndex = currentIndex;
            }
            else
            {
                candidates.SetValidity(currentIndex, false);
            }
        }
    }

    // Explainer:
    // This is responsible for building the branches in the DFA that will be used to match a
    // concrete endpoint based on the Accept-Encoding header of the request.
    // To give you an idea lets explain this through a sample.
    // Say we have the following endpoints:
    // 1 - Resource.css - [ Accept-Encoding: gzip ]
    // 2 - Resource.css - []
    // 3 - Resource.css - [ Accept-Encoding: br ]
    // 4 - {**catchall} - []
    // We need to build a tree that looks like this:
    // root -> gzip -> [ Resource.css (1), Resource.css (2), CatchAll (4) ]
    //      -> br -> [ Resource.css (3), Resource.css (2), CatchAll (4) ]
    //      -> identity -> [ Resource.css (2), CatchAll (4) ]
    //      -> *, "" -> [ Resource.css (2), CatchAll (4) ]
    // The explanation is as follows:
    // * Each node in the tree must have a key, and the list of endpoints that can be matched if that key is selected.
    // * For example, if the Accept-Encoding header is "gzip" then we should select the gzip node, then the gzip endpoint, the "identity" endpoint and the catchall endpoint
    //   are the nodes that are still candidates. This is because a policy later on might invalidate the gzip endpoint (and the algorithm never backtracks).
    // * If we get to the bottom of the tree, then the priority rules get to apply. Endpoints with the metadata will be preferred over those without it, and in case
    //   both of them have the metadata, the quality of the metadata will be used to break the tie.
    // * Note that the priority of the route applies first, that is, in the last case, for a request to Resource.css with no Accept-Encoding header, the Resource.css (2)
    //   endpoint will still be selected over the catchall endpoint.
    public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

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
            var metadata = GetMetadataValue(endpoint) ?? DefaultNegotiationValue;
            if (!edges.TryGetValue(metadata, out var _))
            {
                edges.Add(metadata, []);
            }
        }

        // Now in a second loop, add endpoints to these lists.
        // We've enumerated all of the states, so we want to see which states each endpoint matches.
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            var metadata = GetMetadataValue(endpoint) ?? DefaultNegotiationValue;
            if (string.Equals(metadata, DefaultNegotiationValue, StringComparison.OrdinalIgnoreCase))
            {
                // This means that this endpoint does not specify a negotiation value, a default of identity is assumed.
                // Which means that this endpoint is always a candidate.
                foreach (var edge in edges)
                {
                    edge.Value.Add(endpoint);
                }
            }
            else
            {
                var endpointsForType = edges[metadata];
                endpointsForType.Add(endpoint);
            }
        }

        // If after we're done there isn't any endpoint that accepts the default encoding, then we'll synthesize an
        // endpoint that always returns a 406.
        if (!edges.TryGetValue(DefaultNegotiationValue, out var anyEndpoints))
        {
            anyEndpoints = [CreateRejectionEndpoint()];
            edges.Add(DefaultNegotiationValue, anyEndpoints);

            // Add a node to use when there is no negotiation header.
            edges.Add(string.Empty, anyEndpoints);
        }
        else
        {
            // If there is an endpoint that accepts an then it is also used when there is no content type
            edges.Add(string.Empty, anyEndpoints);
        }

        var result = new PolicyNodeEdge[edges.Count];
        var index = 0;
        foreach (var kvp in edges)
        {
            result[index] = new PolicyNodeEdge(
                // Metadata quality is 0 for the edges that don't have metadata as we prefer serving from the endpoints that have metadata
                new NegotiationEdgeKey(kvp.Key, CalculateEndpointQualities(kvp.Value)),
                kvp.Value);
            index++;
        }

        return result;
    }

    private double[] CalculateEndpointQualities(List<Endpoint> values)
    {
        var result = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            result[i] = GetMetadataQuality(values[i]) ?? 0;
        }
        return result;
    }

    internal class NegotiationEdgeKey
    {
        public NegotiationEdgeKey(string negotiationValue, double[] endpointsQuality)
        {
            NegotiationValue = negotiationValue;
            EndpointsQuality = endpointsQuality;
            Array.Sort(EndpointsQuality);
        }

        public string NegotiationValue { get; }
        public double[] EndpointsQuality { get; }
    }

    private static Endpoint CreateRejectionEndpoint() =>
        Http406Endpoint ??= new Endpoint(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                return Task.CompletedTask;
            },
            EndpointMetadataCollection.Empty,
            Http406EndpointDisplayName);

    PolicyJumpTable INodeBuilderPolicy.BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        var destinations = new (string negotiationValue, double quality, int destination)[edges.Count];
        for (var i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            var key = (NegotiationEdgeKey)e.State;
            destinations[i] = (negotiationValue: key.NegotiationValue, quality: Max(key.EndpointsQuality), destination: e.Destination);
        }

        // If any edge matches all negotiation values, then treat that as the 'exit'. This will
        // always happen because we insert a 406 endpoint.
        var noNegotiationHeaderDestination = -1;
        for (var i = 0; i < destinations.Length; i++)
        {
            if (destinations[i].negotiationValue == DefaultNegotiationValue)
            {
                exitDestination = destinations[i].destination;
            }
            if (destinations[i].negotiationValue == "")
            {
                noNegotiationHeaderDestination = destinations[i].destination;
            }
        }

        return CreateTable(exitDestination, destinations, noNegotiationHeaderDestination);
    }

    private static double Max(double[] endpointsQuality)
    {
        if (endpointsQuality.Length == 0)
        {
            throw new InvalidOperationException("No quality values found.");
        }

        var result = endpointsQuality[0];
        for (var i = 1; i < endpointsQuality.Length; i++)
        {
            result = Math.Max(result, endpointsQuality[i]);
        }

        return result;
    }

    private protected abstract NegotiationPolicyJumpTable CreateTable(int exitDestination, (string negotiationValue, double quality, int destination)[] destinations, int noNegotiationHeaderDestination);

    private sealed class NegotiationMetadataEndpointComparer : EndpointMetadataComparer<TNegotiateMetadata>
    {
        protected override int CompareMetadata(TNegotiateMetadata? x, TNegotiateMetadata? y) =>
            (x, y) switch
            {
                (not null, not null) => (1 - x.Quality).CompareTo(1 - y.Quality),
                _ => base.CompareMetadata(x, y)
            };
    }

    internal abstract class NegotiationPolicyJumpTable : PolicyJumpTable
    {
        private readonly int _defaultNegotiationValueDestination;
        private readonly int _noNegotiationValueDestination;
        private readonly string _negotiationHeader;

        public NegotiationPolicyJumpTable(string negotiationHeader, int anyNegotiationValueDestination, int noNegotiationValueDestination)
        {
            _defaultNegotiationValueDestination = anyNegotiationValueDestination;
            _noNegotiationValueDestination = noNegotiationValueDestination;
            _negotiationHeader = negotiationHeader;
        }

        public override int GetDestination(HttpContext httpContext)
        {
            var header = httpContext.Request.Headers[_negotiationHeader];
            if (StringValues.IsNullOrEmpty(header) ||
                !StringWithQualityHeaderValue.TryParseList(header, out var values) || values.Count == 0)
            {
                return _noNegotiationValueDestination;
            }

            var currentQuality = 0.0d;
            string? currentSelectedValue = null;
            var selectedDestination = _defaultNegotiationValueDestination;

            // Iterate over the list of values to find the best match. First we use the quality on the header value.
            // If that quality is equal to the current quality, we use the server quality to break the tie.
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                var valueQuality = value.Quality ?? 1.0;
                if (valueQuality == 0)
                {
                    // 0 means that the client doesn't want this representation.
                    continue;
                }
                if (valueQuality < currentQuality)
                {
                    // We've already found a better match.
                    continue;
                }

                var valueToken = value.Value.Value ?? null;
                if (valueQuality > currentQuality)
                {
                    var newDestination = GetDestination(valueToken);
                    if (newDestination != -1)
                    {
                        currentSelectedValue = valueToken;
                        selectedDestination = newDestination;
                        currentQuality = valueQuality;
                        continue;
                    }
                }

                if (valueQuality == currentQuality)
                {
                    var currentServerQuality = GetQuality(currentSelectedValue);
                    var newServerQuality = GetQuality(valueToken);
                    if (newServerQuality > currentServerQuality)
                    {
                        var newDestination = GetDestination(valueToken);
                        if (newDestination != -1)
                        {
                            currentSelectedValue = valueToken;
                            selectedDestination = newDestination;
                            currentQuality = valueQuality;
                            continue;
                        }
                    }
                }
            }

            return selectedDestination;
        }

        protected abstract int GetDestination(string? value);

        protected abstract double GetQuality(string? value);
    }
}
