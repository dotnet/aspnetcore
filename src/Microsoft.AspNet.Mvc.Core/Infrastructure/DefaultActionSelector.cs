// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class DefaultActionSelector : IActionSelector
    {
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorsCollectionProvider;
        private readonly IActionSelectorDecisionTreeProvider _decisionTreeProvider;
        private readonly IActionConstraintProvider[] _actionConstraintProviders;
        private ILogger _logger;

        public DefaultActionSelector(
            IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider,
            IActionSelectorDecisionTreeProvider decisionTreeProvider,
            IEnumerable<IActionConstraintProvider> actionConstraintProviders,
            ILoggerFactory loggerFactory)
        {
            _actionDescriptorsCollectionProvider = actionDescriptorsCollectionProvider;
            _decisionTreeProvider = decisionTreeProvider;
            _actionConstraintProviders = actionConstraintProviders.OrderBy(item => item.Order).ToArray();
            _logger = loggerFactory.CreateLogger<DefaultActionSelector>();
        }

        public Task<ActionDescriptor> SelectAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var tree = _decisionTreeProvider.DecisionTree;
            var matchingRouteConstraints = tree.Select(context.RouteData.Values);

            var candidates = new List<ActionSelectorCandidate>();

            // Perf: Avoid allocations
            for (var i = 0; i < matchingRouteConstraints.Count; i++)
            {
                var action = matchingRouteConstraints[i];
                var constraints = GetConstraints(context.HttpContext, action);
                candidates.Add(new ActionSelectorCandidate(action, constraints));
            }

            var matchingActionConstraints =
                EvaluateActionConstraints(context, candidates, startingOrder: null);

            List<ActionDescriptor> matchingActions = null;
            if (matchingActionConstraints != null)
            {
                matchingActions = new List<ActionDescriptor>(matchingActionConstraints.Count);
                // Perf: Avoid allocations
                for (var i = 0; i < matchingActionConstraints.Count; i++)
                {
                    var candidate = matchingActionConstraints[i];
                    matchingActions.Add(candidate.Action);
                }
            }

            var finalMatches = SelectBestActions(matchingActions);

            if (finalMatches == null || finalMatches.Count == 0)
            {
                return Task.FromResult<ActionDescriptor>(null);
            }
            else if (finalMatches.Count == 1)
            {
                var selectedAction = finalMatches[0];

                return Task.FromResult(selectedAction);
            }
            else
            {
                var actionNames = string.Join(
                    Environment.NewLine,
                    finalMatches.Select(a => a.DisplayName));

                _logger.AmbiguousActions(actionNames);

                var message = Resources.FormatDefaultActionSelector_AmbiguousActions(
                    Environment.NewLine,
                    actionNames);

                throw new AmbiguousActionException(message);
            }
        }

        /// <summary>
        /// Returns the set of best matching actions.
        /// </summary>
        /// <param name="actions">The set of actions that satisfy all constraints.</param>
        /// <returns>A list of the best matching actions.</returns>
        protected virtual IReadOnlyList<ActionDescriptor> SelectBestActions(IReadOnlyList<ActionDescriptor> actions)
        {
            return actions;
        }

        private IReadOnlyList<ActionSelectorCandidate> EvaluateActionConstraints(
            RouteContext context,
            IReadOnlyList<ActionSelectorCandidate> candidates,
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

            // If we don't find a 'next' then there's nothing left to do.
            if (order == null)
            {
                return candidates;
            }

            // Since we have a constraint to process, bisect the set of actions into those with and without a
            // constraint for the 'current order'.
            var actionsWithConstraint = new List<ActionSelectorCandidate>();
            var actionsWithoutConstraint = new List<ActionSelectorCandidate>();

            var constraintContext = new ActionConstraintContext();
            constraintContext.Candidates = candidates;
            constraintContext.RouteContext = context;

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
                                _logger.ConstraintMismatch(
                                    candidate.Action.DisplayName,
                                    candidate.Action.Id,
                                    constraint);
                                break;
                            }
                        }
                    }
                }

                if (isMatch && foundMatchingConstraint)
                {
                    actionsWithConstraint.Add(candidate);
                }
                else if (isMatch)
                {
                    actionsWithoutConstraint.Add(candidate);
                }
            }

            // If we have matches with constraints, those are 'better' so try to keep processing those
            if (actionsWithConstraint.Count > 0)
            {
                var matches = EvaluateActionConstraints(context, actionsWithConstraint, order);
                if (matches?.Count > 0)
                {
                    return matches;
                }
            }

            // If the set of matches with constraints can't work, then process the set without constraints.
            if (actionsWithoutConstraint.Count == 0)
            {
                return null;
            }
            else
            {
                return EvaluateActionConstraints(context, actionsWithoutConstraint, order);
            }
        }

        // This method attempts to ensure that the route that's about to generate a link will generate a link
        // to an existing action. This method is called by a route (through MvcApplication) prior to generating
        // any link - this gives WebFX a chance to 'veto' the values provided by a route.
        //
        // This method does not take httpmethod or dynamic action constraints into account.
        public virtual bool HasValidAction(VirtualPathContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ProvidedValues == null)
            {
                // We need the route's values to be able to double check our work.
                return false;
            }

            var tree = _decisionTreeProvider.DecisionTree;
            var matchingRouteConstraints = tree.Select(context.ProvidedValues);

            return matchingRouteConstraints.Count > 0;
        }

        private IReadOnlyList<ActionDescriptor> GetActions()
        {
            var descriptors = _actionDescriptorsCollectionProvider.ActionDescriptors;

            if (descriptors == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull("ActionDescriptors",
                                                               _actionDescriptorsCollectionProvider.GetType()));
            }

            return descriptors.Items;
        }

        private IReadOnlyList<IActionConstraint> GetConstraints(HttpContext httpContext, ActionDescriptor action)
        {
            if (action.ActionConstraints == null || action.ActionConstraints.Count == 0)
            {
                return null;
            }

            var items = new List<ActionConstraintItem>(action.ActionConstraints.Count);
            for (var i = 0; i < action.ActionConstraints.Count; i++)
            {
                items.Add(new ActionConstraintItem(action.ActionConstraints[i]));
            }

            var context = new ActionConstraintProviderContext(httpContext, action, items);
            for (var i = 0; i < _actionConstraintProviders.Length; i++)
            {
                _actionConstraintProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _actionConstraintProviders.Length - 1; i >= 0; i--)
            {
                _actionConstraintProviders[i].OnProvidersExecuted(context);
            }

            var count = 0;
            for (var i = 0; i < context.Results.Count; i++)
            {
                if (context.Results[i].Constraint != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return null;
            }

            var results = new IActionConstraint[count];
            for (int i = 0, j = 0; i < context.Results.Count; i++)
            {
                var constraint = context.Results[i].Constraint;
                if (constraint != null)
                {
                    results[j++] = constraint;
                }
            }

            return results;
        }
    }
}
