// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IInlineConstraintResolver _inlineConstraintResolver;
        private readonly ObjectPool<UriBuildingContext> _objectPool;
        private LinkGenerationDecisionTree _allMatchesLinkGenerationTree;
        private IDictionary<string, LinkGenerationDecisionTree> _namedMatches;

        public RouteValuesBasedEndpointFinder(
            CompositeEndpointDataSource endpointDataSource,
            ObjectPool<UriBuildingContext> objectPool,
            IInlineConstraintResolver inlineConstraintResolver)
        {
            _endpointDataSource = endpointDataSource;
            _objectPool = objectPool;
            _inlineConstraintResolver = inlineConstraintResolver;

            BuildOutboundMatches();
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

        private void BuildOutboundMatches()
        {
            var (allOutboundMatches, namedOutboundMatches) = GetOutboundMatches();
            _namedMatches = GetNamedMatches(namedOutboundMatches);
            _allMatchesLinkGenerationTree = new LinkGenerationDecisionTree(allOutboundMatches.ToArray());
        }

        private (IEnumerable<OutboundMatch>, IDictionary<string, List<OutboundMatch>>) GetOutboundMatches()
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
                Precedence = RoutePrecedence.ComputeOutbound(endpoint.ParsedTemplate),
                RequiredLinkValues = endpoint.RequiredValues,
                RouteTemplate = endpoint.ParsedTemplate,
                Data = endpoint,
                RouteName = routeNameMetadata?.Name,
            };

            // TODO: review. These route constriants should be constructed when the endpoint
            // is built. This way they can be checked for validity on app startup too
            var constraintBuilder = new RouteConstraintBuilder(
                _inlineConstraintResolver,
                endpoint.ParsedTemplate.TemplateText);
            foreach (var parameter in endpoint.ParsedTemplate.Parameters)
            {
                if (parameter.InlineConstraints != null)
                {
                    if (parameter.IsOptional)
                    {
                        constraintBuilder.SetOptional(parameter.Name);
                    }

                    foreach (var constraint in parameter.InlineConstraints)
                    {
                        constraintBuilder.AddResolvedConstraint(parameter.Name, constraint.Constraint);
                    }
                }
            }
            entry.Constraints = constraintBuilder.Build();
            entry.Defaults = endpoint.Defaults;
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

        // Used only to hook up link generation, and it doesn't need to do anything.
        private class NullRouter : IRouter
        {
            public static readonly NullRouter Instance = new NullRouter();

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                return null;
            }

            public Task RouteAsync(RouteContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
