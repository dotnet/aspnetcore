// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// An <see cref="MatcherPolicy"/> that implements filtering and selection by
    /// the HTTP method of a request.
    /// </summary>
    public sealed class HttpMethodMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy, IEndpointSelectorPolicy
    {
        // Used in tests
        internal static readonly string OriginHeader = "Origin";
        internal static readonly string AccessControlRequestMethod = "Access-Control-Request-Method";
        internal static readonly string PreflightHttpMethod = "OPTIONS";

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

        private bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
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
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            // Returning a 405 here requires us to return keep track of all 'seen' HTTP methods. We allocate to
            // keep track of this beause we either need to keep track of the HTTP methods or keep track of the
            // endpoints - both allocate.
            //
            // Those code only runs in the presence of dynamic endpoints anyway.
            //
            // We want to return a 405 iff we eliminated ALL of the currently valid endpoints due to HTTP method
            // mismatch.
            bool? needs405Endpoint = null;
            HashSet<string> methods = null;

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
                    string.Equals(httpMethod, PreflightHttpMethod, StringComparison.OrdinalIgnoreCase) &&
                    headers.ContainsKey(HeaderNames.Origin) &&
                    headers.TryGetValue(HeaderNames.AccessControlRequestMethod, out var accessControlRequestMethod) &&
                    !StringValues.IsNullOrEmpty(accessControlRequestMethod))
                {
                    needs405Endpoint = false; // We don't return a 405 for a CORS preflight request when the endpoints accept CORS preflight.
                    httpMethod = accessControlRequestMethod;
                }

                var matched = false;
                for (var j = 0; j < metadata.HttpMethods.Count; j++)
                {
                    var candidateMethod = metadata.HttpMethods[j];
                    if (!string.Equals(httpMethod, candidateMethod, StringComparison.OrdinalIgnoreCase))
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
                httpContext.SetEndpoint(CreateRejectionEndpoint(methods.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)));
                httpContext.Request.RouteValues = null;
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
            if (!edges.TryGetValue(new EdgeKey(AnyMethod, false), out var matches))
            {
                // Methods sorted for testability.
                var endpoint = CreateRejectionEndpoint(allHttpMethods);
                matches = new List<Endpoint>() { endpoint, };
                edges[new EdgeKey(AnyMethod, false)] = matches;
            }

            var policyNodeEdges = new PolicyNodeEdge[edges.Count];
            var index = 0;
            foreach (var kvp in edges)
            {
                policyNodeEdges[index++] = new PolicyNodeEdge(kvp.Key, kvp.Value);
            }

            return policyNodeEdges;

            (IReadOnlyList<string> httpMethods, bool acceptCorsPreflight) GetHttpMethods(Endpoint e)
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
            Dictionary<string, int> destinations = null;
            Dictionary<string, int> corsPreflightDestinations = null;
            for (var i = 0; i < edges.Count; i++)
            {
                // We create this data, so it's safe to cast it.
                var key = (EdgeKey)edges[i].State;
                if (key.IsCorsPreflightRequest)
                {
                    if (corsPreflightDestinations == null)
                    {
                        corsPreflightDestinations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    }

                    corsPreflightDestinations.Add(key.HttpMethod, edges[i].Destination);
                }
                else
                {
                    if (destinations == null)
                    {
                        destinations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    }

                    destinations.Add(key.HttpMethod, edges[i].Destination);
                }
            }

            int corsPreflightExitDestination = exitDestination;
            if (corsPreflightDestinations != null && corsPreflightDestinations.TryGetValue(AnyMethod, out var matchesAnyVerb))
            {
                // If we have endpoints that match any HTTP method, use that as the exit.
                corsPreflightExitDestination = matchesAnyVerb;
                corsPreflightDestinations.Remove(AnyMethod);
            }

            if (destinations != null && destinations.TryGetValue(AnyMethod, out matchesAnyVerb))
            {
                // If we have endpoints that match any HTTP method, use that as the exit.
                exitDestination = matchesAnyVerb;
                destinations.Remove(AnyMethod);
            }

            return new HttpMethodPolicyJumpTable(
                exitDestination,
                destinations,
                corsPreflightExitDestination,
                corsPreflightDestinations);
        }

        private Endpoint CreateRejectionEndpoint(IEnumerable<string> httpMethods)
        {
            var allow = string.Join(", ", httpMethods);
            return new Endpoint(
                (context) =>
                {
                    context.Response.StatusCode = 405;

                    // Prevent ArgumentException from duplicate key if header already added, such as when the
                    // request is re-executed by an error handler (see https://github.com/dotnet/aspnetcore/issues/6415)
                    context.Response.Headers[HeaderNames.Allow] = allow;

                    return Task.CompletedTask;
                },
                EndpointMetadataCollection.Empty,
                Http405EndpointDisplayName);
        }

        private static bool ContainsHttpMethod(List<string> httpMethods, string httpMethod)
        {
            for (var i = 0; i < httpMethods.Count; i++)
            {
                if (string.Equals(httpMethods[i], httpMethod, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private class HttpMethodPolicyJumpTable : PolicyJumpTable
        {
            private readonly int _exitDestination;
            private readonly Dictionary<string, int> _destinations;
            private readonly int _corsPreflightExitDestination;
            private readonly Dictionary<string, int> _corsPreflightDestinations;

            private readonly bool _supportsCorsPreflight;

            public HttpMethodPolicyJumpTable(
                int exitDestination,
                Dictionary<string, int> destinations,
                int corsPreflightExitDestination,
                Dictionary<string, int> corsPreflightDestinations)
            {
                _exitDestination = exitDestination;
                _destinations = destinations;
                _corsPreflightExitDestination = corsPreflightExitDestination;
                _corsPreflightDestinations = corsPreflightDestinations;

                _supportsCorsPreflight = _corsPreflightDestinations != null && _corsPreflightDestinations.Count > 0;
            }

            public override int GetDestination(HttpContext httpContext)
            {
                int destination;

                var httpMethod = httpContext.Request.Method;
                var headers = httpContext.Request.Headers;
                if (_supportsCorsPreflight &&
                    string.Equals(httpMethod, PreflightHttpMethod, StringComparison.OrdinalIgnoreCase) &&
                    headers.ContainsKey(HeaderNames.Origin) &&
                    headers.TryGetValue(HeaderNames.AccessControlRequestMethod, out var accessControlRequestMethod) &&
                    !StringValues.IsNullOrEmpty(accessControlRequestMethod))
                {
                    return _corsPreflightDestinations != null &&
                        _corsPreflightDestinations.TryGetValue(accessControlRequestMethod, out destination)
                        ? destination
                        : _corsPreflightExitDestination;
                }

                return _destinations != null &&
                    _destinations.TryGetValue(httpMethod, out destination) ? destination : _exitDestination;
            }
        }

        private class HttpMethodMetadataEndpointComparer : EndpointMetadataComparer<IHttpMethodMetadata>
        {
            protected override int CompareMetadata(IHttpMethodMetadata x, IHttpMethodMetadata y)
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
                var compare = HttpMethod.CompareTo(other.HttpMethod);
                if (compare != 0)
                {
                    return compare;
                }

                return IsCorsPreflightRequest.CompareTo(other.IsCorsPreflightRequest);
            }

            public int CompareTo(object obj)
            {
                return CompareTo((EdgeKey)obj);
            }

            public bool Equals(EdgeKey other)
            {
                return
                    IsCorsPreflightRequest == other.IsCorsPreflightRequest &&
                    string.Equals(HttpMethod, other.HttpMethod, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                var other = obj as EdgeKey?;
                return other == null ? false : Equals(other.Value);
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(IsCorsPreflightRequest ? 1 : 0);
                hash.Add(HttpMethod, StringComparer.Ordinal);
                return hash;
            }

            // Used in GraphViz output.
            public override string ToString()
            {
                return IsCorsPreflightRequest ? $"CORS: {HttpMethod}" : $"HTTP: {HttpMethod}";
            }
        }
    }
}
