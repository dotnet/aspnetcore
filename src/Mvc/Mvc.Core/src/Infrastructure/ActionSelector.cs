// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A default <see cref="IActionSelector"/> implementation.
/// </summary>
internal partial class ActionSelector : IActionSelector
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly ActionConstraintCache _actionConstraintCache;
    private readonly ILogger _logger;

    private ActionSelectionTable<ActionDescriptor>? _cache;

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

    public ActionDescriptor? SelectBestCandidate(RouteContext context, IReadOnlyList<ActionDescriptor> candidates)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (candidates == null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        var finalMatches = EvaluateActionConstraints(context, candidates);

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
            Log.AmbiguousActions(_logger, actionNames);

            var message = Resources.FormatDefaultActionSelector_AmbiguousActions(
                Environment.NewLine,
                actionNames);

            throw new AmbiguousActionException(message);
        }
    }

    private IReadOnlyList<ActionDescriptor>? EvaluateActionConstraints(
        RouteContext context,
        IReadOnlyList<ActionDescriptor> actions)
    {
        var actionsCount = actions.Count;
        var candidates = new List<ActionSelectorCandidate>(actionsCount);

        // Perf: Avoid allocations
        for (var i = 0; i < actionsCount; i++)
        {
            var action = actions[i];
            var constraints = _actionConstraintCache.GetActionConstraints(context.HttpContext, action);
            candidates.Add(new ActionSelectorCandidate(action, constraints));
        }

        var matches = EvaluateActionConstraintsCore(context, candidates, startingOrder: null);

        List<ActionDescriptor>? results = null;
        if (matches != null)
        {
            var matchesCount = matches.Count;
            results = new List<ActionDescriptor>(matchesCount);
            // Perf: Avoid allocations
            for (var i = 0; i < matchesCount; i++)
            {
                var candidate = matches[i];
                results.Add(candidate.Action);
            }
        }

        return results;
    }

    private IReadOnlyList<ActionSelectorCandidate>? EvaluateActionConstraintsCore(
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
                            Log.ConstraintMismatch(
                                _logger,
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

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Error, "Request matched multiple actions resulting in ambiguity. Matching actions: {AmbiguousActions}", EventName = "AmbiguousActions")]
        public static partial void AmbiguousActions(ILogger logger, string ambiguousActions);

        [LoggerMessage(2, LogLevel.Debug, "Action '{ActionName}' with id '{ActionId}' did not match the constraint '{ActionConstraint}'", EventName = "ConstraintMismatch")]
        public static partial void ConstraintMismatch(ILogger logger, string? actionName, string actionId, IActionConstraint actionConstraint);
    }
}
