// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// An <see cref="MatcherPolicy"/> that implements filtering and selection by
/// the HTTP method of a request.
/// </summary>
public sealed class HttpMethodMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy, IEndpointSelectorPolicy
{
    // Used in tests
    internal static readonly string PreflightHttpMethod = HttpMethods.Options;

    // Used in tests
    internal const string Http405EndpointDisplayName = "405 HTTP Method Not Supported";

    // Used in tests
    internal const string AnyMethod = "*";

    /// <summary>
    /// For framework use only.
    /// </summary>
    public IComparer<Endpoint> Comparer => new HttpMethodMetadataEndpointComparer();

    // The order value is chosen to be less than 0, so that it comes before naively
    // written policies.
    /// <summary>
    /// For framework use only.
    /// </summary>
    public override int Order => -1000;

    bool INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (ContainsDynamicEndpoints(endpoints))
        {
            return false;
        }

        return AppliesToEndpointsCore(endpoints);
    }

    bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // When the node contains dynamic endpoints we can't make any assumptions.
        return ContainsDynamicEndpoints(endpoints);
    }

    private static bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
    {
        for (var i = 0; i < endpoints.Count; i++)
        {
            if (endpoints[i].Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="candidates"></param>
    /// <returns></returns>
    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(candidates);

        // Returning a 405 here requires us to return keep track of all 'seen' HTTP methods. We allocate to
        // keep track of this because we either need to keep track of the HTTP methods or keep track of the
        // endpoints - both allocate.
        //
        // Those code only runs in the presence of dynamic endpoints anyway.
        //
        // We want to return a 405 iff we eliminated ALL of the currently valid endpoints due to HTTP method
        // mismatch.
        bool? needs405Endpoint = null;
        HashSet<string>? methods = null;

        for (var i = 0; i < candidates.Count; i++)
        {
            // We do this check first for consistency with how 405 is implemented for the graph version
            // of this code. We still want to know if any endpoints in this set require an HTTP method
            // even if those endpoints are already invalid - hence the null-check.
            var metadata = candidates[i].Endpoint?.Metadata.GetMetadata<IHttpMethodMetadata>();
            if (metadata == null || metadata.HttpMethods.Count == 0)
            {
                // Can match any method.
                needs405Endpoint = false;
                continue;
            }

            // Saw a valid endpoint.
            needs405Endpoint = needs405Endpoint ?? true;

            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var httpMethod = httpContext.Request.Method;
            var headers = httpContext.Request.Headers;
            if (metadata.AcceptCorsPreflight &&
                HttpMethods.Equals(httpMethod, PreflightHttpMethod) &&
                headers.ContainsKey(HeaderNames.Origin) &&
                headers.TryGetValue(HeaderNames.AccessControlRequestMethod, out var accessControlRequestMethod) &&
                !StringValues.IsNullOrEmpty(accessControlRequestMethod))
            {
                needs405Endpoint = false; // We don't return a 405 for a CORS preflight request when the endpoints accept CORS preflight.
                httpMethod = accessControlRequestMethod.ToString();
            }

            var matched = false;
            for (var j = 0; j < metadata.HttpMethods.Count; j++)
            {
                var candidateMethod = metadata.HttpMethods[j];
                if (!HttpMethods.Equals(httpMethod, candidateMethod))
                {
                    methods = methods ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    methods.Add(candidateMethod);
                    continue;
                }

                matched = true;
                needs405Endpoint = false;
                break;
            }

            if (!matched)
            {
                candidates.SetValidity(i, false);
            }
        }

        if (needs405Endpoint == true)
        {
            // We saw some endpoints coming in, and we eliminated them all.
            httpContext.SetEndpoint(CreateRejectionEndpoint(methods?.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)));
            httpContext.Request.RouteValues = null!;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
    {
        // The algorithm here is designed to be preserve the order of the endpoints
        // while also being relatively simple. Preserving order is important.

        // First, build a dictionary of all possible HTTP method/CORS combinations
        // that exist in this list of endpoints.
        //
        // For now we're just building up the set of keys. We don't add any endpoints
        // to lists now because we don't want ordering problems.
        var allHttpMethods = new List<string>();
        var edges = new Dictionary<EdgeKey, List<Endpoint>>();
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            var (httpMethods, acceptCorsPreFlight) = GetHttpMethods(endpoint);

            // If the action doesn't list HTTP methods then it supports all methods.
            // In this phase we use a sentinel value to represent the *other* HTTP method
            // a state that represents any HTTP method that doesn't have a match.
            if (httpMethods.Count == 0)
            {
                httpMethods = new[] { AnyMethod, };
            }

            for (var j = 0; j < httpMethods.Count; j++)
            {
                // An endpoint that allows CORS reqests will match both CORS and non-CORS
                // so we model it as both.
                var httpMethod = httpMethods[j];
                var key = new EdgeKey(httpMethod, acceptCorsPreFlight);
                if (!edges.ContainsKey(key))
                {
                    edges.Add(key, new List<Endpoint>());
                }

                // An endpoint that allows CORS reqests will match both CORS and non-CORS
                // so we model it as both.
                if (acceptCorsPreFlight)
                {
                    key = new EdgeKey(httpMethod, false);
                    if (!edges.ContainsKey(key))
                    {
                        edges.Add(key, new List<Endpoint>());
                    }
                }

                // Also if it's not the *any* method key, then track it.
                if (!string.Equals(AnyMethod, httpMethod, StringComparison.OrdinalIgnoreCase))
                {
                    if (!ContainsHttpMethod(allHttpMethods, httpMethod))
                    {
                        allHttpMethods.Add(httpMethod);
                    }
                }
            }
        }

        allHttpMethods.Sort(StringComparer.OrdinalIgnoreCase);

        // Now in a second loop, add endpoints to these lists. We've enumerated all of
        // the states, so we want to see which states this endpoint matches.
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            var (httpMethods, acceptCorsPreFlight) = GetHttpMethods(endpoint);

            if (httpMethods.Count == 0)
            {
                // OK this means that this endpoint matches *all* HTTP methods.
                // So, loop and add it to all states.
                foreach (var kvp in edges)
                {
                    if (acceptCorsPreFlight || !kvp.Key.IsCorsPreflightRequest)
                    {
                        kvp.Value.Add(endpoint);
                    }
                }
            }
            else
            {
                // OK this endpoint matches specific methods.
                for (var j = 0; j < httpMethods.Count; j++)
                {
                    var httpMethod = httpMethods[j];
                    var key = new EdgeKey(httpMethod, acceptCorsPreFlight);

                    edges[key].Add(endpoint);

                    // An endpoint that allows CORS reqests will match both CORS and non-CORS
                    // so we model it as both.
                    if (acceptCorsPreFlight)
                    {
                        key = new EdgeKey(httpMethod, false);
                        edges[key].Add(endpoint);
                    }
                }
            }
        }

        // Adds a very low priority endpoint that will reject the request with
        // a 405 if nothing else can handle this verb. This is only done if
        // no other actions exist that handle the 'all verbs'.
        //
        // The rationale for this is that we want to report a 405 if none of
        // the supported methods match, but we don't want to report a 405 in a
        // case where an application defines an endpoint that handles all verbs, but
        // a constraint rejects the request, or a complex segment fails to parse. We
        // consider a case like that a 'user input validation' failure  rather than
        // a semantic violation of HTTP.
        //
        // This will make 405 much more likely in API-focused applications, and somewhat
        // unlikely in a traditional MVC application. That's good.
        //
        // We don't bother returning a 405 when the CORS preflight method doesn't exist.
        // The developer calling the API will see it as a CORS error, which is fine because
        // there isn't an endpoint to check for a CORS policy.
        if (!edges.TryGetValue(new EdgeKey(AnyMethod, false), out _))
        {
            // Methods sorted for testability.
            var endpoint = CreateRejectionEndpoint(allHttpMethods);
            var matches = new List<Endpoint>() { endpoint, };
            edges[new EdgeKey(AnyMethod, false)] = matches;
        }

        var policyNodeEdges = new PolicyNodeEdge[edges.Count];
        var index = 0;
        foreach (var kvp in edges)
        {
            policyNodeEdges[index++] = new PolicyNodeEdge(kvp.Key, kvp.Value);
        }

        return policyNodeEdges;

        static (IReadOnlyList<string> httpMethods, bool acceptCorsPreflight) GetHttpMethods(Endpoint e)
        {
            var metadata = e.Metadata.GetMetadata<IHttpMethodMetadata>();
            return metadata == null ? (Array.Empty<string>(), false) : (metadata.HttpMethods, metadata.AcceptCorsPreflight);
        }
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    /// <param name="exitDestination"></param>
    /// <param name="edges"></param>
    /// <returns></returns>
    public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
    {
        List<KeyValuePair<string, int>>? destinations = null;
        List<KeyValuePair<string, int>>? corsPreflightDestinations = null;
        var corsPreflightExitDestination = exitDestination;

        for (var i = 0; i < edges.Count; i++)
        {
            // We create this data, so it's safe to cast it.
            var key = (EdgeKey)edges[i].State;
            var destination = edges[i].Destination;

            if (key.IsCorsPreflightRequest)
            {
                ProcessEdge(key.HttpMethod, destination, ref corsPreflightExitDestination, ref corsPreflightDestinations);
            }
            else
            {
                ProcessEdge(key.HttpMethod, destination, ref exitDestination, ref destinations);
            }
        }

        if (destinations?.Count == 1)
        {
            // If there is only a single valid HTTP method then use an optimized jump table.
            // It avoids unnecessary dictionary lookups with the method name.
            var httpMethodDestination = destinations[0];
            var method = httpMethodDestination.Key;
            var destination = httpMethodDestination.Value;
            var supportsCorsPreflight = false;
            var corsPreflightDestination = 0;

            if (corsPreflightDestinations?.Count > 0)
            {
                supportsCorsPreflight = true;
                corsPreflightDestination = corsPreflightDestinations[0].Value;
            }

            return new HttpMethodSingleEntryPolicyJumpTable(
                exitDestination,
                method,
                destination,
                supportsCorsPreflight,
                corsPreflightExitDestination,
                corsPreflightDestination);
        }
        else
        {
            return new HttpMethodDictionaryPolicyJumpTable(
                new HttpMethodDestinationsLookup(destinations ?? new(), exitDestination),
                corsPreflightDestinations != null ? new HttpMethodDestinationsLookup(corsPreflightDestinations, corsPreflightExitDestination) : null);
        }

        static void ProcessEdge(string httpMethod, int destination, ref int exitDestination, ref List<KeyValuePair<string, int>>? destinations)
        {
            // If we have endpoints that match any HTTP method, use that as the exit.
            if (string.Equals(httpMethod, AnyMethod, StringComparison.OrdinalIgnoreCase))
            {
                exitDestination = destination;
            }
            else
            {

                destinations ??= new();
                destinations.Add(KeyValuePair.Create(httpMethod, destination));
            }
        }
    }

    private static Endpoint CreateRejectionEndpoint(IEnumerable<string>? httpMethods)
    {
        var allow = httpMethods is null ? string.Empty : string.Join(", ", httpMethods);
        return new Endpoint(
            (context) =>
            {
                // Prevent ArgumentException from duplicate key if header already added, such as when the
                // request is re-executed by an error handler (see https://github.com/dotnet/aspnetcore/issues/6415)
                context.Response.Headers.Allow = allow;
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return Task.CompletedTask;
            },
            EndpointMetadataCollection.Empty,
            Http405EndpointDisplayName);
    }

    private static bool ContainsHttpMethod(List<string> httpMethods, string httpMethod)
    {
        var methods = CollectionsMarshal.AsSpan(httpMethods);
        for (var i = 0; i < methods.Length; i++)
        {
            // This is a fast path for when everything is using static HttpMethods instances.
            if (object.ReferenceEquals(methods[i], httpMethod))
            {
                return true;
            }
        }

        for (var i = 0; i < methods.Length; i++)
        {
            if (HttpMethods.Equals(methods[i], httpMethod))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsCorsPreflightRequest(HttpContext httpContext, string httpMethod, out StringValues accessControlRequestMethod)
    {
        accessControlRequestMethod = default;
        var headers = httpContext.Request.Headers;

        return HttpMethods.Equals(httpMethod, PreflightHttpMethod) &&
            headers.ContainsKey(HeaderNames.Origin) &&
            headers.TryGetValue(HeaderNames.AccessControlRequestMethod, out accessControlRequestMethod) &&
            !StringValues.IsNullOrEmpty(accessControlRequestMethod);
    }

    private sealed class HttpMethodMetadataEndpointComparer : EndpointMetadataComparer<IHttpMethodMetadata>
    {
        protected override int CompareMetadata(IHttpMethodMetadata? x, IHttpMethodMetadata? y)
        {
            // Ignore the metadata if it has an empty list of HTTP methods.
            return base.CompareMetadata(
                x?.HttpMethods.Count > 0 ? x : null,
                y?.HttpMethods.Count > 0 ? y : null);
        }
    }

    internal readonly struct EdgeKey : IEquatable<EdgeKey>, IComparable<EdgeKey>, IComparable
    {
        // Note that in contrast with the metadata, the edge represents a possible state change
        // rather than a list of what's allowed. We represent CORS and non-CORS requests as separate
        // states.
        public readonly bool IsCorsPreflightRequest;
        public readonly string HttpMethod;

        public EdgeKey(string httpMethod, bool isCorsPreflightRequest)
        {
            HttpMethod = httpMethod;
            IsCorsPreflightRequest = isCorsPreflightRequest;
        }

        // These are comparable so they can be sorted in tests.
        public int CompareTo(EdgeKey other)
        {
            var compare = string.Compare(HttpMethod, other.HttpMethod, StringComparison.Ordinal);
            if (compare != 0)
            {
                return compare;
            }

            return IsCorsPreflightRequest.CompareTo(other.IsCorsPreflightRequest);
        }

        public int CompareTo(object? obj)
        {
            return CompareTo((EdgeKey)obj!);
        }

        public bool Equals(EdgeKey other)
        {
            return
                IsCorsPreflightRequest == other.IsCorsPreflightRequest &&
                HttpMethods.Equals(HttpMethod, other.HttpMethod);
        }

        public override bool Equals(object? obj)
        {
            var other = obj as EdgeKey?;
            return other == null ? false : Equals(other.Value);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(IsCorsPreflightRequest ? 1 : 0);
            hash.Add(HttpMethod, StringComparer.Ordinal);
            return hash.ToHashCode();
        }

        // Used in GraphViz output.
        public override string ToString()
        {
            return IsCorsPreflightRequest ? $"CORS: {HttpMethod}" : $"HTTP: {HttpMethod}";
        }
    }
}
