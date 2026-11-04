// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Mvc.Routing;

// This is a bridge that allows us to execute IActionConstraint instance when
// used with Matcher.
internal sealed class ActionConstraintMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
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

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // We can skip over action constraints when they aren't any for this set
        // of endpoints. This happens once on startup so it removes this component
        // from the code path in most scenarios.
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            var action = endpoint.Metadata.GetMetadata<ActionDescriptor>();
            if (action?.ActionConstraints is IList<IActionConstraintMetadata> { Count: > 0 } constraints && HasSignificantActionConstraint(constraints))
            {
                // We need to check for some specific action constraint implementations.
                // We've implemented consumes, and HTTP method support inside endpoint routing, so
                // we don't need to run an 'action constraint phase' if those are the only constraints.
                return true;
            }
        }

        return false;

        static bool HasSignificantActionConstraint(IList<IActionConstraintMetadata> constraints)
        {
            for (var i = 0; i < constraints.Count; i++)
            {
                var actionConstraint = constraints[i];
                if (actionConstraint.GetType() == typeof(HttpMethodActionConstraint))
                {
                    // This one is OK, we implement this in endpoint routing.
                }
                else if (actionConstraint.GetType() == typeof(ConsumesAttribute))
                {
                    // This one is OK, we implement this in endpoint routing.
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidateSet)
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

        return Task.CompletedTask;
    }

    // This is almost the same as the code in ActionSelector, but we can't really share the logic
    // because we need to track the index of each candidate - and, each candidate has its own route
    // values.
    private IReadOnlyList<(int index, ActionSelectorCandidate candidate)>? EvaluateActionConstraints(
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

                IReadOnlyList<IActionConstraint>? constraints = Array.Empty<IActionConstraint>();
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

    private static IReadOnlyList<(int index, ActionSelectorCandidate candidate)>? EvaluateActionConstraintsCore(
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

        var constraintContext = new ActionConstraintContext
        {
            Candidates = items.Select(i => i.candidate).ToArray()
        };

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

                        ref var candidate = ref candidateSet[item.index];

                        var routeData = new RouteData(candidate.Values!);

                        var dataTokens = candidate.Endpoint.Metadata.GetMetadata<IDataTokensMetadata>()?.DataTokens;

                        if (dataTokens != null)
                        {
                            // Set the data tokens if there are any for this candidate
                            routeData.PushState(router: null, values: null, dataTokens: new RouteValueDictionary(dataTokens));
                        }

                        // Before we run the constraint, we need to initialize the route values.
                        // In endpoint routing, the route values are per-endpoint.
                        constraintContext.RouteContext = new RouteContext(httpContext)
                        {
                            RouteData = routeData,
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
