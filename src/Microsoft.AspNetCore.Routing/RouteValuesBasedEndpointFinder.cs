// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.EndpointFinders;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing
{
    internal class RouteValuesBasedEndpointFinder : IEndpointFinder<RouteValuesBasedEndpointFinderContext>
    {
        private readonly CompositeEndpointDataSource _endpointDataSource;
        private readonly ObjectPool<UriBuildingContext> _objectPool;
        private LinkGenerationDecisionTree _allMatchesLinkGenerationTree;
        private IDictionary<string, LinkGenerationDecisionTree> _namedMatches;

        public RouteValuesBasedEndpointFinder(
            CompositeEndpointDataSource endpointDataSource,
            ObjectPool<UriBuildingContext> objectPool)
        {
            _endpointDataSource = endpointDataSource;
            _objectPool = objectPool;

            // Build initial matches
            BuildOutboundMatches();

            // Register for changes in endpoints
            Extensions.Primitives.ChangeToken.OnChange(
                () => _endpointDataSource.ChangeToken,
                () => HandleChange());
        }

        public IEnumerable<Endpoint> FindEndpoints(RouteValuesBasedEndpointFinderContext context)
        {
            IEnumerable<OutboundMatchResult> matchResults = null;
            if (string.IsNullOrEmpty(context.RouteName))
            {
                matchResults = _allMatchesLinkGenerationTree.GetMatches(
                    context.ExplicitValues,
                    context.AmbientValues);
            }
            else if (_namedMatches.TryGetValue(context.RouteName, out var linkGenerationTree))
            {
                matchResults = linkGenerationTree.GetMatches(
                    context.ExplicitValues,
                    context.AmbientValues);
            }

            if (matchResults == null || !matchResults.Any())
            {
                return Array.Empty<Endpoint>();
            }

            return matchResults
                .Select(matchResult => matchResult.Match)
                .Select(match => (MatcherEndpoint)match.Entry.Data);
        }

        private void HandleChange()
        {
            // rebuild the matches
            BuildOutboundMatches();

            // re-register the callback as the change token is one time use only and a new change token
            // is produced every time
            Extensions.Primitives.ChangeToken.OnChange(
                () => _endpointDataSource.ChangeToken,
                () => HandleChange());
        }

        private void BuildOutboundMatches()
        {
            // Refresh the matches in the case where a datasource's endpoints changes. The following is OK to do
            // as refresh of new endpoints happens within a lock and also these fields are not publicly accessible.
            var (allMatches, namedMatches) = GetOutboundMatches();
            _namedMatches = GetNamedMatches(namedMatches);
            _allMatchesLinkGenerationTree = new LinkGenerationDecisionTree(allMatches.ToArray());
        }

        protected virtual (IEnumerable<OutboundMatch>, IDictionary<string, List<OutboundMatch>>) GetOutboundMatches()
        {
            var allOutboundMatches = new List<OutboundMatch>();
            var namedOutboundMatches = new Dictionary<string, List<OutboundMatch>>(StringComparer.OrdinalIgnoreCase);

            var endpoints = _endpointDataSource.Endpoints.OfType<MatcherEndpoint>();
            foreach (var endpoint in endpoints)
            {
                var entry = CreateOutboundRouteEntry(endpoint);

                var outboundMatch = new OutboundMatch() { Entry = entry };
                allOutboundMatches.Add(outboundMatch);

                if (string.IsNullOrEmpty(entry.RouteName))
                {
                    continue;
                }

                List<OutboundMatch> matches;
                if (!namedOutboundMatches.TryGetValue(entry.RouteName, out matches))
                {
                    matches = new List<OutboundMatch>();
                    namedOutboundMatches.Add(entry.RouteName, matches);
                }
                matches.Add(outboundMatch);
            }

            return (allOutboundMatches, namedOutboundMatches);
        }

        private OutboundRouteEntry CreateOutboundRouteEntry(MatcherEndpoint endpoint)
        {
            var routeNameMetadata = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            var entry = new OutboundRouteEntry()
            {
                Handler = NullRouter.Instance,
                Order = endpoint.Order,
                Precedence = RoutePrecedence.ComputeOutbound(endpoint.RoutePattern),
                RequiredLinkValues = new RouteValueDictionary(endpoint.RequiredValues),
                RouteTemplate = new RouteTemplate(endpoint.RoutePattern),
                Data = endpoint,
                RouteName = routeNameMetadata?.Name,
            };
            entry.Defaults = new RouteValueDictionary(endpoint.RoutePattern.Defaults);
            return entry;
        }

        private IDictionary<string, LinkGenerationDecisionTree> GetNamedMatches(
            IDictionary<string, List<OutboundMatch>> namedOutboundMatches)
        {
            var result = new Dictionary<string, LinkGenerationDecisionTree>(StringComparer.OrdinalIgnoreCase);
            foreach (var namedOutboundMatch in namedOutboundMatches)
            {
                result.Add(namedOutboundMatch.Key, new LinkGenerationDecisionTree(namedOutboundMatch.Value.ToArray()));
            }
            return result;
        }
    }
}
