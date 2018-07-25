// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public sealed class HttpMethodEndpointSelectorPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
    {
        // Used in tests
        internal const string Http405EndpointDisplayName = "405 HTTP Method Not Supported";

        // Used in tests
        internal const string AnyMethod = "*";

        public IComparer<Endpoint> Comparer => new HttpMethodMetadataEndpointComparer();

        // The order value is chosen to be less than 0, so that it comes before naively
        // written policies.
        public override int Order => -1000;

        public bool AppliesToNode(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            for (var i = 0; i < endpoints.Count; i++)
            {
                if (endpoints[i].Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Any() == true)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            // The algorithm here is designed to be preserve the order of the endpoints
            // while also being relatively simple. Preserving order is important.
            var allHttpMethods = endpoints
                    .SelectMany(e => GetHttpMethods(e))
                    .Distinct()
                    .OrderBy(m => m); // Sort for testability

            var dictionary = new Dictionary<string, List<Endpoint>>();
            foreach (var httpMethod in allHttpMethods)
            {
                dictionary.Add(httpMethod, new List<Endpoint>());
            }

            dictionary.Add(AnyMethod, new List<Endpoint>());

            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];

                var httpMethods = GetHttpMethods(endpoint);
                if (httpMethods.Count == 0)
                {
                    // This endpoint suports all HTTP methods.
                    foreach (var kvp in dictionary)
                    {
                        kvp.Value.Add(endpoint);
                    }

                    continue;
                }

                for (var j = 0; j < httpMethods.Count; j++)
                {
                    dictionary[httpMethods[j]].Add(endpoint);
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
            if (dictionary[AnyMethod].Count == 0)
            {
                dictionary[AnyMethod].Add(CreateRejectionEndpoint(allHttpMethods));
            }

            var edges = new List<PolicyNodeEdge>();
            foreach (var kvp in dictionary)
            {
                edges.Add(new PolicyNodeEdge(kvp.Key, kvp.Value));
            }

            return edges;

            IReadOnlyList<string> GetHttpMethods(Endpoint e)
            {
                return e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? Array.Empty<string>();
            }
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < edges.Count; i++)
            {
                // We create this data, so it's safe to cast it to a string.
                dictionary.Add((string)edges[i].State, edges[i].Destination);
            }

            if (dictionary.TryGetValue(AnyMethod, out var matchesAnyVerb))
            {
                // If we have endpoints that match any HTTP method, use that as the exit.
                exitDestination = matchesAnyVerb;
                dictionary.Remove(AnyMethod);
            }

            return new DictionaryPolicyJumpTable(exitDestination, dictionary);
        }

        private Endpoint CreateRejectionEndpoint(IEnumerable<string> httpMethods)
        {
            var allow = string.Join(", ", httpMethods);
            return new MatcherEndpoint(
                (next) => (context) =>
                {
                    context.Response.StatusCode = 405;
                    context.Response.Headers.Add("Allow", allow);
                    return Task.CompletedTask;
                },
                RoutePatternFactory.Parse("/"),
                new RouteValueDictionary(),
                0,
                EndpointMetadataCollection.Empty,
                Http405EndpointDisplayName);
        }

        private class DictionaryPolicyJumpTable : PolicyJumpTable
        {
            private readonly int _exitDestination;
            private readonly Dictionary<string, int> _destinations;

            public DictionaryPolicyJumpTable(int exitDestination, Dictionary<string, int> destinations)
            {
                _exitDestination = exitDestination;
                _destinations = destinations;
            }

            public override int GetDestination(HttpContext httpContext)
            {
                var httpMethod = httpContext.Request.Method;
                return _destinations.TryGetValue(httpMethod, out var destination) ? destination : _exitDestination;
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
    }
}
