// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    // This is a bridge that allows us to execute IActionConstraint instance when
    // used with Matcher.
    internal class ActionConstraintMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        private static readonly IReadOnlyList<Endpoint> EmptyEndpoints = Array.Empty<Endpoint>();

        // We need to be able to run IActionConstraints on Endpoints that aren't associated
        // with an action. This is a sentinel value we use when the endpoint isn't from MVC.
        internal static readonly ActionDescriptor NonAction = new ActionDescriptor();
        
        private readonly ActionConstraintCache _actionConstraintCache;

        public ActionConstraintMatcherPolicy(ActionConstraintCache actionConstraintCache)
        {
            _actionConstraintCache = actionConstraintCache;
        }

        // Run really late.
        public override int Order => 100000;

        // Internal for testing
        internal bool ShouldRunActionConstraints => _actionConstraintCache.CurrentCache.HasActionConstraints;

        public Task ApplyAsync(HttpContext httpContext, EndpointSelectorContext context, CandidateSet candidateSet)
        {
            // PERF: we can skip over action constraints if there aren't any app-wide.
            //
            // Running action constraints (or just checking for them) in a candidate set
            // is somewhat expensive compared to other routing operations. This should only
            // happen if user-code adds action constraints.
            if (ShouldRunActionConstraints)
            {
                ApplyActionConstraints(httpContext, candidateSet);
            }

            return Task.CompletedTask;
        }
        
        private void ApplyActionConstraints(
            HttpContext httpContext,
            CandidateSet candidateSet)
        {
            var finalMatches = EvaluateActionConstraints(httpContext, candidateSet);

            // We've computed the set of actions that still apply (and their indices)
            // First, mark everything as invalid, and then mark everything in the matching
            // set as valid. This is O(2n) vs O(n**2)
            for (var i = 0; i < candidateSet.Count; i++)
            {
                candidateSet.SetValidity(i, false);
            }

            if (finalMatches != null)
            {
                for (var i = 0; i < finalMatches.Count; i++)
                {
                    candidateSet.SetValidity(finalMatches[i].index, true);
                }
            }
        }

        // This is almost the same as the code in ActionSelector, but we can't really share the logic
        // because we need to track the index of each candidate - and, each candidate has its own route
        // values.
        private IReadOnlyList<(int index, ActionSelectorCandidate candidate)> EvaluateActionConstraints(
            HttpContext httpContext,
            CandidateSet candidateSet)
        {
            var items = new List<(int index, ActionSelectorCandidate candidate)>();

            // We want to execute a group at a time (based on score) so keep track of the score that we've seen.
            int? score = null;

            // Perf: Avoid allocations
            for (var i = 0; i < candidateSet.Count; i++)
            {
                if (candidateSet.IsValidCandidate(i))
                {
                    ref var candidate = ref candidateSet[i];
                    if (score != null && score != candidate.Score)
                    {
                        // This is the end of a group.
                        var matches = EvaluateActionConstraintsCore(httpContext, candidateSet, items, startingOrder: null);
                        if (matches?.Count > 0)
                        {
                            return matches;
                        }

                        // If we didn't find matches, then reset.
                        items.Clear();
                    }

                    score = candidate.Score;

                    // If we get here, this is either the first endpoint or the we just (unsuccessfully)
                    // executed constraints for a group.
                    //
                    // So keep adding constraints.
                    var endpoint = candidate.Endpoint;
                    var actionDescriptor = endpoint.Metadata.GetMetadata<ActionDescriptor>();

                    IReadOnlyList<IActionConstraint> constraints = Array.Empty<IActionConstraint>();
                    if (actionDescriptor != null)
                    {
                        constraints = _actionConstraintCache.GetActionConstraints(httpContext, actionDescriptor);
                    }

                    // Capture the index. We need this later to look up the endpoint/route values.
                    items.Add((i, new ActionSelectorCandidate(actionDescriptor ?? NonAction, constraints)));
                }
            }

            // Handle residue
            return EvaluateActionConstraintsCore(httpContext, candidateSet, items, startingOrder: null);
        }

        private IReadOnlyList<(int index, ActionSelectorCandidate candidate)> EvaluateActionConstraintsCore(
            HttpContext httpContext,
            CandidateSet candidateSet,
            IReadOnlyList<(int index, ActionSelectorCandidate candidate)> items,
            int? startingOrder)
        {
            // Find the next group of constraints to process. This will be the lowest value of
            // order that is higher than startingOrder.
            int? order = null;

            // Perf: Avoid allocations
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var constraints = item.candidate.Constraints;
                if (constraints != null)
                {
                    for (var j = 0; j < constraints.Count; j++)
                    {
                        var constraint = constraints[j];
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
                return items;
            }

            // Since we have a constraint to process, bisect the set of endpoints into those with and without a
            // constraint for the current order.
            var endpointsWithConstraint = new List<(int index, ActionSelectorCandidate candidate)>();
            var endpointsWithoutConstraint = new List<(int index, ActionSelectorCandidate candidate)>();

            var constraintContext = new ActionConstraintContext();
            constraintContext.Candidates = items.Select(i => i.candidate).ToArray();

            // Perf: Avoid allocations
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isMatch = true;
                var foundMatchingConstraint = false;

                var constraints = item.candidate.Constraints;
                if (constraints != null)
                {
                    constraintContext.CurrentCandidate = item.candidate;
                    for (var j = 0; j < constraints.Count; j++)
                    {
                        var constraint = constraints[j];
                        if (constraint.Order == order)
                        {
                            foundMatchingConstraint = true;

                            // Before we run the constraint, we need to initialize the route values.
                            // In endpoint routing, the route values are per-endpoint.
                            constraintContext.RouteContext = new RouteContext(httpContext)
                            {
                                RouteData = new RouteData(candidateSet[item.index].Values),
                            };
                            if (!constraint.Accept(constraintContext))
                            {
                                isMatch = false;
                                break;
                            }
                        }
                    }
                }

                if (isMatch && foundMatchingConstraint)
                {
                    endpointsWithConstraint.Add(item);
                }
                else if (isMatch)
                {
                    endpointsWithoutConstraint.Add(item);
                }
            }

            // If we have matches with constraints, those are better so try to keep processing those
            if (endpointsWithConstraint.Count > 0)
            {
                var matches = EvaluateActionConstraintsCore(httpContext, candidateSet, endpointsWithConstraint, order);
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
                return EvaluateActionConstraintsCore(httpContext, candidateSet, endpointsWithoutConstraint, order);
            }
        }
    }
}
