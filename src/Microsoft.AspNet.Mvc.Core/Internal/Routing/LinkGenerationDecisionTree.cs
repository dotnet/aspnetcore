// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Internal.DecisionTree;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Internal.Routing
{
    // A decision tree that matches link generation entries based on route data.
    public class LinkGenerationDecisionTree
    {
        private readonly DecisionTreeNode<AttributeRouteLinkGenerationEntry> _root;

        public LinkGenerationDecisionTree(IReadOnlyList<AttributeRouteLinkGenerationEntry> entries)
        {
            _root = DecisionTreeBuilder<AttributeRouteLinkGenerationEntry>.GenerateTree(
                entries,
                new AttributeRouteLinkGenerationEntryClassifier());
        }

        public List<AttributeRouteLinkGenerationEntry> GetMatches(VirtualPathContext context)
        {
            var results = new List<AttributeRouteLinkGenerationEntry>();
            Walk(results, context, _root);
            results.Sort(AttributeRouteLinkGenerationEntryComparer.Instance);
            return results;
        }

        // We need to recursively walk the decision tree based on the provided route data
        // (context.Values + context.AmbientValues) to find all entries that match. This process is 
        // virtually identical to action selection.
        //
        // Each entry has a collection of 'required link values' that must be satisfied. These are
        // key-value pairs that make up the decision tree.
        //
        // A 'require link value' is considered satisfied IF:
        //  1. The value in context.Values matches the required value OR
        //  2. There is no value in context.Values and the value in context.AmbientValues matches OR
        //  3. The required value is 'null' and there is no value in context.Values.
        //
        // Ex:
        //  entry requires { area = null, controller = Store, action = Buy }
        //  context.Values = { controller = Store, action = Buy }
        //  context.AmbientValues = { area = Help, controller = AboutStore, action = HowToBuyThings }
        //
        //  In this case the entry is a match. The 'controller' and 'action' are both supplied by context.Values,
        //  and the 'area' is satisfied because there's NOT a value in context.Values. It's OK to ignore ambient
        //  values in link generation.
        //
        //  If another entry existed like { area = Help, controller = Store, action = Buy }, this would also
        //  match.
        //
        // The decision tree uses a tree data structure to execute these rules across all candidates at once.
        private void Walk(
            List<AttributeRouteLinkGenerationEntry> results,
            VirtualPathContext context,
            DecisionTreeNode<AttributeRouteLinkGenerationEntry> node)
        {
            // Any entries in node.Matches have had all their required values satisfied, so add them
            // to the results.
            for (var i = 0; i < node.Matches.Count; i++)
            {
                results.Add(node.Matches[i]);
            }

            for (var i = 0; i < node.Criteria.Count; i++)
            {
                var criterion = node.Criteria[i];
                var key = criterion.Key;

                object value;
                if (context.Values.TryGetValue(key, out value))
                {
                    DecisionTreeNode<AttributeRouteLinkGenerationEntry> branch;
                    if (criterion.Branches.TryGetValue(value ?? string.Empty, out branch))
                    {
                        Walk(results, context, branch);
                    }
                }
                else
                {
                    // If a value wasn't explicitly supplied, match BOTH the ambient value and the empty value
                    // if an ambient value was supplied.
                    DecisionTreeNode<AttributeRouteLinkGenerationEntry> branch;
                    if (context.AmbientValues.TryGetValue(key, out value) &&
                        !criterion.Branches.Comparer.Equals(value, string.Empty))
                    {
                        if (criterion.Branches.TryGetValue(value, out branch))
                        {
                            Walk(results, context, branch);
                        }
                    }

                    if (criterion.Branches.TryGetValue(string.Empty, out branch))
                    {
                        Walk(results, context, branch);
                    }
                }
            }
        }

        private class AttributeRouteLinkGenerationEntryClassifier : IClassifier<AttributeRouteLinkGenerationEntry>
        {
            public AttributeRouteLinkGenerationEntryClassifier()
            {
                ValueComparer = new RouteValueEqualityComparer();
            }

            public IEqualityComparer<object> ValueComparer { get; private set; }

            public IDictionary<string, DecisionCriterionValue> GetCriteria(AttributeRouteLinkGenerationEntry item)
            {
                var results = new Dictionary<string, DecisionCriterionValue>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in item.RequiredLinkValues)
                {
                    results.Add(kvp.Key, new DecisionCriterionValue(kvp.Value ?? string.Empty, isCatchAll: false));
                }

                return results;
            }
        }

        private class AttributeRouteLinkGenerationEntryComparer : IComparer<AttributeRouteLinkGenerationEntry>
        {
            public static readonly AttributeRouteLinkGenerationEntryComparer Instance = 
                new AttributeRouteLinkGenerationEntryComparer();

            public int Compare(AttributeRouteLinkGenerationEntry x, AttributeRouteLinkGenerationEntry y)
            {
                if (x.Order != y.Order)
                {
                    return x.Order.CompareTo(y.Order);
                }

                if (x.Precedence != y.Precedence)
                {
                    return x.Precedence.CompareTo(y.Precedence);
                }

                return StringComparer.Ordinal.Compare(x.TemplateText, y.TemplateText);
            }
        }
    }
}