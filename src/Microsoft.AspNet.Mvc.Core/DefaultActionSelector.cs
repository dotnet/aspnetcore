// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionSelector : IActionSelector
    {
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorsCollectionProvider;
        private readonly IActionSelectorDecisionTreeProvider _decisionTreeProvider;
        private readonly INestedProviderManager<ActionConstraintProviderContext> _actionConstraintProvider;
        private ILogger _logger;

        public DefaultActionSelector(
            [NotNull] IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider,
            [NotNull] IActionSelectorDecisionTreeProvider decisionTreeProvider,
            [NotNull] INestedProviderManager<ActionConstraintProviderContext> actionConstraintProvider,
            [NotNull] ILoggerFactory loggerFactory)
        {
            _actionDescriptorsCollectionProvider = actionDescriptorsCollectionProvider;
            _decisionTreeProvider = decisionTreeProvider;
            _actionConstraintProvider = actionConstraintProvider;
            _logger = loggerFactory.Create<DefaultActionSelector>();
        }

        public Task<ActionDescriptor> SelectAsync([NotNull] RouteContext context)
        {
            using (_logger.BeginScope("DefaultActionSelector.SelectAsync"))
            {
                var tree = _decisionTreeProvider.DecisionTree;
                var matchingRouteConstraints = tree.Select(context.RouteData.Values);

                var candidates = new List<ActionSelectorCandidate>();
                foreach (var action in matchingRouteConstraints)
                {
                    var constraints = GetConstraints(action);
                    candidates.Add(new ActionSelectorCandidate(action, constraints));
                }

                var matchingActionConstraints =
                    EvaluateActionConstraints(context, candidates, startingOrder: null);

                List<ActionDescriptor> matchingActions = null;
                if (matchingActionConstraints != null)
                {
                    matchingActions = new List<ActionDescriptor>(matchingActionConstraints.Count);
                    foreach (var candidate in matchingActionConstraints)
                    {
                        matchingActions.Add(candidate.Action);
                    }
                }

                var finalMatches = SelectBestActions(matchingActions);

                if (finalMatches == null || finalMatches.Count == 0)
                {
                    if (_logger.IsEnabled(TraceType.Verbose))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingActionConstraints = matchingActions,
                            FinalMatches = finalMatches,
                        });
                    }

                    return Task.FromResult<ActionDescriptor>(null);
                }
                else if (finalMatches.Count == 1)
                {
                    var selectedAction = finalMatches[0];

                    if (_logger.IsEnabled(TraceType.Verbose))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingActionConstraints = matchingActions,
                            FinalMatches = finalMatches,
                            SelectedAction = selectedAction
                        });
                    }

                    return Task.FromResult(selectedAction);
                }
                else
                {
                    if (_logger.IsEnabled(TraceType.Verbose))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingActionConstraints = matchingActions,
                            FinalMatches = finalMatches,
                        });
                    }

                    var actionNames = string.Join(
                        Environment.NewLine,
                        finalMatches.Select(a => a.DisplayName));

                    var message = Resources.FormatDefaultActionSelector_AmbiguousActions(
                        Environment.NewLine,
                        actionNames);

                    throw new AmbiguousActionException(message);
                }
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
            foreach (var candidate in candidates)
            {
                if (candidate.Constraints != null)
                {
                    foreach (var constraint in candidate.Constraints)
                    {
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

            foreach (var candidate in candidates)
            {
                var isMatch = true;
                var foundMatchingConstraint = false;

                if (candidate.Constraints != null)
                {
                    constraintContext.CurrentCandidate = candidate;
                    foreach (var constraint in candidate.Constraints)
                    {
                        if (constraint.Order == order)
                        {
                            foundMatchingConstraint = true;

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
        public virtual bool HasValidAction([NotNull] VirtualPathContext context)
        {
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

        private IReadOnlyList<IActionConstraint> GetConstraints(ActionDescriptor action)
        {
            if (action.ActionConstraints == null || action.ActionConstraints.Count == 0)
            {
                return null;
            }

            var items = action.ActionConstraints.Select(c => new ActionConstraintItem(c)).ToList();
            var context = new ActionConstraintProviderContext(action, items);

            _actionConstraintProvider.Invoke(context);

            return 
                context.Results
                .Where(item => item.Constraint != null)
                .Select(item => item.Constraint)
                .ToList();
        }
    }
}
