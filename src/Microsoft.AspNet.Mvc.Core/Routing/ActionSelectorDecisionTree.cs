// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Mvc.Internal.DecisionTree;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <inheritdoc />
    public class ActionSelectionDecisionTree : IActionSelectionDecisionTree
    {
        private readonly DecisionTreeNode<ActionDescriptor> _root;

        /// <summary>
        /// Creates a new <see cref="ActionSelectionDecisionTree"/>.
        /// </summary>
        /// <param name="actions">The <see cref="ActionDescriptorsCollection"/>.</param>
        public ActionSelectionDecisionTree(ActionDescriptorsCollection actions)
        {
            Version = actions.Version;

            _root = DecisionTreeBuilder<ActionDescriptor>.GenerateTree(
                actions.Items,
                new ActionDescriptorClassifier());
        }

        /// <inheritdoc />
        public int Version { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<ActionDescriptor> Select(IDictionary<string, object> routeValues)
        {
            var results = new List<ActionDescriptor>();
            Walk(results, routeValues, _root);

            // If we have a match that isn't using catch-all, then it's considered better than matches with catch all
            // so filter those out.
            var hasNonCatchAll = false;

            // The common case for MVC has no catch-alls, so avoid allocating.
            List<ActionDescriptor> filtered = null;

            foreach (var action in results)
            {
                var actionHasCatchAll = false;
                if (action.RouteConstraints != null)
                {
                    foreach (var constraint in action.RouteConstraints)
                    {
                        if (constraint.KeyHandling == RouteKeyHandling.CatchAll)
                        {
                            actionHasCatchAll = true;
                            break;
                        }
                    }
                }

                if (hasNonCatchAll && actionHasCatchAll)
                {
                    // Do nothing - we've already found a better match.
                }
                else if (actionHasCatchAll)
                {
                    if (filtered == null)
                    {
                        filtered = new List<ActionDescriptor>();
                    }

                    filtered.Add(action);
                }
                else if (hasNonCatchAll)
                {
                    Contract.Assert(filtered != null);
                    filtered.Add(action);
                }
                else
                {
                    // This is the first non-catch-all we've found.
                    hasNonCatchAll = true;

                    if (filtered == null)
                    {
                        filtered = new List<ActionDescriptor>();
                    }
                    else
                    {
                        filtered.Clear();
                    }

                    filtered.Add(action);
                }
            }

            return filtered ?? results;
        }

        private void Walk(
            List<ActionDescriptor> results,
            IDictionary<string, object> routeValues,
            DecisionTreeNode<ActionDescriptor> node)
        {
            for (var i = 0; i < node.Matches.Count; i++)
            {
                results.Add(node.Matches[i]);
            }

            for (var i = 0; i < node.Criteria.Count; i++)
            {
                var criterion = node.Criteria[i];
                var key = criterion.Key;

                object value;
                var hasValue = routeValues.TryGetValue(key, out value);

                DecisionTreeNode<ActionDescriptor> branch;
                if (criterion.Branches.TryGetValue(value ?? string.Empty, out branch))
                {
                    Walk(results, routeValues, branch);
                }

                // If there's a fallback node we always need to process it when we have a value. We'll prioritize 
                // non-fallback matches later in the process.
                if (hasValue && criterion.Fallback != null)
                {
                    Walk(results, routeValues, criterion.Fallback);
                }
            }
        }

        private class ActionDescriptorClassifier : IClassifier<ActionDescriptor>
        {
            public ActionDescriptorClassifier()
            {
                ValueComparer = new RouteValueEqualityComparer();
            }

            public IEqualityComparer<object> ValueComparer { get; private set; }

            public IDictionary<string, DecisionCriterionValue> GetCriteria(ActionDescriptor item)
            {
                var results = new Dictionary<string, DecisionCriterionValue>(StringComparer.OrdinalIgnoreCase);

                if (item.RouteConstraints != null)
                {
                    foreach (var constraint in item.RouteConstraints)
                    {
                        DecisionCriterionValue value;
                        if (constraint.KeyHandling == RouteKeyHandling.CatchAll)
                        {
                            value = new DecisionCriterionValue(value: null, isCatchAll: true);
                        }
                        else if (constraint.KeyHandling == RouteKeyHandling.DenyKey)
                        {
                            // null and string.Empty are equivalent for route values, so just treat nulls as
                            // string.Empty.
                            value = new DecisionCriterionValue(value: string.Empty, isCatchAll: false);
                        }
                        else if (constraint.KeyHandling == RouteKeyHandling.RequireKey)
                        {
                            value = new DecisionCriterionValue(value: constraint.RouteValue, isCatchAll: false);
                        }
                        else
                        {
                            // We'd already have failed before getting here. The RouteDataActionConstraint constructor
                            // would throw.
#if ASPNET50
                            throw new InvalidEnumArgumentException(
                                "item",
                                (int)constraint.KeyHandling,
                                typeof(RouteKeyHandling));
#else
                            throw new ArgumentOutOfRangeException("item");
#endif
                        }

                        results.Add(constraint.RouteKey, value);
                    }
                }

                return results;
            }
        }
    }
}
