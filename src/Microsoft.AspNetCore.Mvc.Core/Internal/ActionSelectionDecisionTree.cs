// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.DecisionTree;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <inheritdoc />
    public class ActionSelectionDecisionTree : IActionSelectionDecisionTree
    {
        private readonly DecisionTreeNode<ActionDescriptor> _root;

        /// <summary>
        /// Creates a new <see cref="ActionSelectionDecisionTree"/>.
        /// </summary>
        /// <param name="actions">The <see cref="ActionDescriptorCollection"/>.</param>
        public ActionSelectionDecisionTree(ActionDescriptorCollection actions)
        {
            Version = actions.Version;

            var conventionalRoutedActions = actions.Items.Where(a => a.AttributeRouteInfo?.Template == null).ToArray();
            _root = DecisionTreeBuilder<ActionDescriptor>.GenerateTree(
                conventionalRoutedActions,
                new ActionDescriptorClassifier());
        }

        /// <inheritdoc />
        public int Version { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<ActionDescriptor> Select(IDictionary<string, object> routeValues)
        {
            var results = new List<ActionDescriptor>();
            Walk(results, routeValues, _root);

            return results;
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
                routeValues.TryGetValue(key, out value);

                DecisionTreeNode<ActionDescriptor> branch;
                if (criterion.Branches.TryGetValue(value ?? string.Empty, out branch))
                {
                    Walk(results, routeValues, branch);
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

                if (item.RouteValues != null)
                {
                    foreach (var kvp in item.RouteValues)
                    {
                        // null and string.Empty are equivalent for route values, so just treat nulls as
                        // string.Empty.
                        results.Add(kvp.Key, new DecisionCriterionValue(kvp.Value ?? string.Empty));
                    }
                }

                return results;
            }
        }
    }
}
