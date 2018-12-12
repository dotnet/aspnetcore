// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    internal class RouteValuesAddressScheme : IEndpointAddressScheme<RouteValuesAddress>
    {
        private readonly EndpointDataSource _dataSource;
        private LinkGenerationDecisionTree _allMatchesLinkGenerationTree;
        private Dictionary<string, List<OutboundMatchResult>> _namedMatchResults;

        public RouteValuesAddressScheme(EndpointDataSource dataSource)
        {
            _dataSource = dataSource;

            // Build initial matches
            BuildOutboundMatches();

            // Register for changes in endpoints
            ChangeToken.OnChange(
                _dataSource.GetChangeToken,
                HandleChange);
        }

        public IEnumerable<Endpoint> FindEndpoints(RouteValuesAddress address)
        {
            IList<OutboundMatchResult> matchResults = null;
            if (string.IsNullOrEmpty(address.RouteName))
            {
                matchResults = _allMatchesLinkGenerationTree.GetMatches(
                    address.ExplicitValues,
                    address.AmbientValues);
            }
            else if (_namedMatchResults.TryGetValue(address.RouteName, out var namedMatchResults))
            {
                matchResults = namedMatchResults;
            }

            if (matchResults != null)
            {
                var matchCount = matchResults.Count;
                if (matchCount > 0)
                {
                    if (matchResults.Count == 1)
                    {
                        // Special case having a single result to avoid creating iterator state machine
                        return new[] { (RouteEndpoint)matchResults[0].Match.Entry.Data };
                    }
                    else
                    {
                        // Use separate method since one cannot have regular returns in an iterator method
                        return GetEndpoints(matchResults, matchCount);
                    }
                }
            }

            return Array.Empty<Endpoint>();
        }

        private static IEnumerable<Endpoint> GetEndpoints(IList<OutboundMatchResult> matchResults, int matchCount)
        {
            for (var i = 0; i < matchCount; i++)
            {
                yield return (RouteEndpoint)matchResults[i].Match.Entry.Data;
            }
        }

        private void HandleChange()
        {
            // rebuild the matches
            BuildOutboundMatches();

            // re-register the callback as the change token is one time use only and a new change token
            // is produced every time
            ChangeToken.OnChange(
                _dataSource.GetChangeToken,
                HandleChange);
        }

        private void BuildOutboundMatches()
        {
            // Refresh the matches in the case where a datasource's endpoints changes. The following is OK to do
            // as refresh of new endpoints happens within a lock and also these fields are not publicly accessible.
            var (allMatches, namedMatchResults) = GetOutboundMatches();
            _namedMatchResults = namedMatchResults;
            _allMatchesLinkGenerationTree = new LinkGenerationDecisionTree(allMatches);
        }

        /// Decision tree is built using the 'required values' of actions.
        /// - When generating a url using route values, decision tree checks the explicitly supplied route values +
        ///   ambient values to see if they have a match for the required-values-based-tree.
        /// - When generating a url using route name, route values for controller, action etc.might not be provided
        ///   (this is expected because as a user I want to avoid writing all those and instead chose to use a
        ///   routename which is quick). So since these values are not provided and might not be even in ambient
        ///   values, decision tree would fail to find a match. So for this reason decision tree is not used for named
        ///   matches. Instead all named matches are returned as is and the LinkGenerator uses a TemplateBinder to
        ///   decide which of the matches can generate a url.
        ///   For example, for a route defined like below with current ambient values like new { controller = "Home",
        ///   action = "Index" }
        ///     "api/orders/{id}",
        ///     routeName: "OrdersApi",
        ///     defaults: new { controller = "Orders", action = "GetById" },
        ///     requiredValues: new { controller = "Orders", action = "GetById" },
        ///   A call to GetLink("OrdersApi", new { id = "10" }) cannot generate url as neither the supplied values or
        ///   current ambient values do not satisfy the decision tree that is built based on the required values.
        protected virtual (List<OutboundMatch>, Dictionary<string, List<OutboundMatchResult>>) GetOutboundMatches()
        {
            var allOutboundMatches = new List<OutboundMatch>();
            var namedOutboundMatchResults = new Dictionary<string, List<OutboundMatchResult>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var endpoint in _dataSource.Endpoints)
            {
                if (!(endpoint is RouteEndpoint routeEndpoint))
                {
                    continue;
                }

                var metadata = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
                if (metadata == null && routeEndpoint.RoutePattern.RequiredValues.Count == 0)
                {
                    continue;
                }

                if (endpoint.Metadata.GetMetadata<ISuppressLinkGenerationMetadata>()?.SuppressLinkGeneration == true)
                {
                    continue;
                }

                var entry = CreateOutboundRouteEntry(
                    routeEndpoint,
                    routeEndpoint.RoutePattern.RequiredValues,
                    metadata?.RouteName);

                var outboundMatch = new OutboundMatch() { Entry = entry };
                allOutboundMatches.Add(outboundMatch);

                if (string.IsNullOrEmpty(entry.RouteName))
                {
                    continue;
                }

                if (!namedOutboundMatchResults.TryGetValue(entry.RouteName, out var matchResults))
                {
                    matchResults = new List<OutboundMatchResult>();
                    namedOutboundMatchResults.Add(entry.RouteName, matchResults);
                }
                matchResults.Add(new OutboundMatchResult(outboundMatch, isFallbackMatch: false));
            }

            return (allOutboundMatches, namedOutboundMatchResults);
        }

        private OutboundRouteEntry CreateOutboundRouteEntry(
            RouteEndpoint endpoint, 
            IReadOnlyDictionary<string, object> requiredValues,
            string routeName)
        {
            var entry = new OutboundRouteEntry()
            {
                Handler = NullRouter.Instance,
                Order = endpoint.Order,
                Precedence = RoutePrecedence.ComputeOutbound(endpoint.RoutePattern),
                RequiredLinkValues = new RouteValueDictionary(requiredValues),
                RouteTemplate = new RouteTemplate(endpoint.RoutePattern),
                Data = endpoint,
                RouteName = routeName,
            };
            entry.Defaults = new RouteValueDictionary(endpoint.RoutePattern.Defaults);
            return entry;
        }
    }
}
