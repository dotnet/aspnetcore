// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A default <see cref="IActionSelector"/> implementation.
    /// </summary>
    internal class ActionSelector : IActionSelector
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly ActionConstraintCache _actionConstraintCache;
        private readonly ILogger _logger;

        private ActionSelectionTable<ActionDescriptor> _cache;

        /// <summary>
        /// Creates a new <see cref="ActionSelector"/>.
        /// </summary>
        /// <param name="actionDescriptorCollectionProvider">
        /// The <see cref="IActionDescriptorCollectionProvider"/>.
        /// </param>
        /// <param name="actionConstraintCache">The <see cref="ActionConstraintCache"/> that
        /// providers a set of <see cref="IActionConstraint"/> instances.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ActionSelector(
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
            ActionConstraintCache actionConstraintCache,
            ILoggerFactory loggerFactory)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _logger = loggerFactory.CreateLogger<ActionSelector>();
            _actionConstraintCache = actionConstraintCache;
        }

        private ActionSelectionTable<ActionDescriptor> Current
        {
            get
            {
                var actions = _actionDescriptorCollectionProvider.ActionDescriptors;
                var cache = Volatile.Read(ref _cache);

                if (cache != null && cache.Version == actions.Version)
                {
                    return cache;
                }

                cache = ActionSelectionTable<ActionDescriptor>.Create(actions);
                Volatile.Write(ref _cache, cache);
                return cache;
            }
        }

        public IReadOnlyList<ActionDescriptor> SelectCandidates(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cache = Current;

            var matches = cache.Select(context.RouteData.Values);
            if (matches.Count > 0)
            {
                return matches;
            }

            _logger.NoActionsMatched(context.RouteData.Values);
            return matches;
        }

        public ActionDescriptor SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var matches = EvaluateActionConstraints(context, candidates);

            var finalMatches = SelectBestActions(matches);
            if (finalMatches == null || finalMatches.Count == 0)
            {
                return null;
            }
            else if (finalMatches.Count == 1)
            {
                var selectedAction = finalMatches[0];

                return selectedAction;
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
        private IReadOnlyList<ActionDescriptor> SelectBestActions(IReadOnlyList<ActionDescriptor> actions)
        {
            return actions;
        }

        private IReadOnlyList<ActionDescriptor> EvaluateActionConstraints(
            RouteContext context,
            IReadOnlyList<ActionDescriptor> actions)
        {
            var candidates = new List<ActionSelectorCandidate>();

            // Perf: Avoid allocations
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var constraints = _actionConstraintCache.GetActionConstraints(context.HttpContext, action);
                candidates.Add(new ActionSelectorCandidate(action, constraints));
            }

            var matches = EvaluateActionConstraintsCore(context, candidates, startingOrder: null);

            List<ActionDescriptor> results = null;
            if (matches != null)
            {
                results = new List<ActionDescriptor>(matches.Count);
                // Perf: Avoid allocations
                for (var i = 0; i < matches.Count; i++)
                {
                    var candidate = matches[i];
                    results.Add(candidate.Action);
                }
            }

            return results;
        }

        private IReadOnlyList<ActionSelectorCandidate> EvaluateActionConstraintsCore(
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

            // If we don't find a next then there's nothing left to do.
            if (order == null)
            {
                return candidates;
            }

            // Since we have a constraint to process, bisect the set of actions into those with and without a
            // constraint for the current order.
            var actionsWithConstraint = new List<ActionSelectorCandidate>();
            var actionsWithoutConstraint = new List<ActionSelectorCandidate>();

            var constraintContext = new ActionConstraintContext
            {
                Candidates = candidates,
                RouteContext = context
            };

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

            // If we have matches with constraints, those are better so try to keep processing those
            if (actionsWithConstraint.Count > 0)
            {
                var matches = EvaluateActionConstraintsCore(context, actionsWithConstraint, order);
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
                return EvaluateActionConstraintsCore(context, actionsWithoutConstraint, order);
            }
        }
    }
}
