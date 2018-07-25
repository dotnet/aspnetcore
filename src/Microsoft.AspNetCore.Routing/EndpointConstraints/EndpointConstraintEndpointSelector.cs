// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.EndpointConstraints
{
    internal class EndpointConstraintEndpointSelector : EndpointSelector
    {
        private static readonly IReadOnlyList<Endpoint> EmptyEndpoints = Array.Empty<Endpoint>();

        private readonly CompositeEndpointDataSource _dataSource;
        private readonly EndpointConstraintCache _endpointConstraintCache;
        private readonly ILogger _logger;

        public EndpointConstraintEndpointSelector(
            CompositeEndpointDataSource dataSource,
            EndpointConstraintCache endpointConstraintCache,
            ILoggerFactory loggerFactory)
        {
            _dataSource = dataSource;
            _logger = loggerFactory.CreateLogger<EndpointSelector>();
            _endpointConstraintCache = endpointConstraintCache;
        }

        public override Task SelectAsync(
            HttpContext httpContext,
            IEndpointFeature feature,
            CandidateSet candidates)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var finalMatches = EvaluateEndpointConstraints(httpContext, candidates);

            if (finalMatches == null || finalMatches.Count == 0)
            {
                return Task.CompletedTask;
            }
            else if (finalMatches.Count == 1)
            {
                var endpoint = finalMatches[0].Endpoint;
                var values = finalMatches[0].Values;

                feature.Endpoint = endpoint;
                feature.Invoker = (endpoint as MatcherEndpoint)?.Invoker;
                feature.Values = values;

                return Task.CompletedTask;
            }
            else
            {
                var endpointNames = string.Join(
                    Environment.NewLine,
                    finalMatches.Select(a => a.Endpoint.DisplayName));

                Log.MatchAmbiguous(_logger, httpContext, finalMatches);

                var message = Resources.FormatAmbiguousEndpoints(
                    Environment.NewLine,
                    string.Join(Environment.NewLine, endpointNames));

                throw new AmbiguousMatchException(message);
            }
        }

        private IReadOnlyList<EndpointSelectorCandidate> EvaluateEndpointConstraints(
            HttpContext context,
            CandidateSet candidateSet)
        {
            var candidates = new List<EndpointSelectorCandidate>();

            // Perf: Avoid allocations
            for (var i = 0; i < candidateSet.Count; i++)
            {
                ref var candidate = ref candidateSet[i];
                if (candidate.IsValidCandidate)
                {
                    var endpoint = candidate.Endpoint;
                    var constraints = _endpointConstraintCache.GetEndpointConstraints(context, endpoint);
                    candidates.Add(new EndpointSelectorCandidate(
                        endpoint,
                        candidate.Score,
                        candidate.Values,
                        constraints));
                }
            }

            var matches = EvaluateEndpointConstraintsCore(context, candidates, startingOrder: null);

            List<EndpointSelectorCandidate> results = null;
            if (matches != null)
            {
                results = new List<EndpointSelectorCandidate>(matches.Count);

                // We need to disambiguate based on 'score' - take the first value of 'score'
                // and then we only copy matches while they have the same score. This accounts
                // for a difference in behavior between new routing and old.
                switch (matches.Count)
                {
                    case 0:
                        break;

                    case 1:
                        results.Add(matches[0]);
                        break;

                    default:
                        var score = matches[0].Score;
                        for (var i = 0; i < matches.Count; i++)
                        {
                            if (matches[i].Score != score)
                            {
                                break;
                            }

                            results.Add(matches[i]);
                        }

                        break;

                }
            }

            return results;
        }

        private IReadOnlyList<EndpointSelectorCandidate> EvaluateEndpointConstraintsCore(
            HttpContext context,
            IReadOnlyList<EndpointSelectorCandidate> candidates,
            int? startingOrder)
        {
            // Find the next group of constraints to process. This will be the lowest value of
            // order that is higher than startingOrder.
            int? order = null;

            // Perf: Avoid allocations
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Constraints != null)
                {
                    for (var j = 0; j < candidate.Constraints.Count; j++)
                    {
                        var constraint = candidate.Constraints[j];
                        if ((startingOrder == null || constraint.Order > startingOrder) &&
                            (order == null || constraint.Order < order))
                        {
                            order = constraint.Order;
                        }
                    }
                }
            }

            // If we don't find a next then there's nothing left to do.
            if (order == null)
            {
                return candidates;
            }

            // Since we have a constraint to process, bisect the set of endpoints into those with and without a
            // constraint for the current order.
            var endpointsWithConstraint = new List<EndpointSelectorCandidate>();
            var endpointsWithoutConstraint = new List<EndpointSelectorCandidate>();

            var constraintContext = new EndpointConstraintContext();
            constraintContext.Candidates = candidates;
            constraintContext.HttpContext = context;

            // Perf: Avoid allocations
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var isMatch = true;
                var foundMatchingConstraint = false;

                if (candidate.Constraints != null)
                {
                    constraintContext.CurrentCandidate = candidate;
                    for (var j = 0; j < candidate.Constraints.Count; j++)
                    {
                        var constraint = candidate.Constraints[j];
                        if (constraint.Order == order)
                        {
                            foundMatchingConstraint = true;

                            if (!constraint.Accept(constraintContext))
                            {
                                isMatch = false;
                                //_logger.ConstraintMismatch(
                                //    candidate.Endpoint.DisplayName,
                                //    candidate.Endpoint.Id,
                                //    constraint);
                                break;
                            }
                        }
                    }
                }

                if (isMatch && foundMatchingConstraint)
                {
                    endpointsWithConstraint.Add(candidate);
                }
                else if (isMatch)
                {
                    endpointsWithoutConstraint.Add(candidate);
                }
            }

            // If we have matches with constraints, those are better so try to keep processing those
            if (endpointsWithConstraint.Count > 0)
            {
                var matches = EvaluateEndpointConstraintsCore(context, endpointsWithConstraint, order);
                if (matches?.Count > 0)
                {
                    return matches;
                }
            }

            // If the set of matches with constraints can't work, then process the set without constraints.
            if (endpointsWithoutConstraint.Count == 0)
            {
                return null;
            }
            else
            {
                return EvaluateEndpointConstraintsCore(context, endpointsWithoutConstraint, order);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, PathString, IEnumerable<string>, Exception> _matchAmbiguous = LoggerMessage.Define<PathString, IEnumerable<string>>(
                LogLevel.Error,
                new EventId(1, "MatchAmbiguous"),
                "Request matched multiple endpoints for request path '{Path}'. Matching endpoints: {AmbiguousEndpoints}");

            public static void MatchAmbiguous(ILogger logger, HttpContext httpContext, IEnumerable<EndpointSelectorCandidate> endpoints)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _matchAmbiguous(logger, httpContext.Request.Path, endpoints.Select(e => e.Endpoint.DisplayName), null);
                }
            }
        }
    }
}