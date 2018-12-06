// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.DecisionTree;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Routing.Internal
{
    // A decision tree that matches link generation entries based on route data.
    public class LinkGenerationDecisionTree
    {
        private readonly DecisionTreeNode<OutboundMatch> _root;

        public LinkGenerationDecisionTree(IReadOnlyList<OutboundMatch> entries)
        {
            _root = DecisionTreeBuilder<OutboundMatch>.GenerateTree(
                entries,
                new OutboundMatchClassifier());
        }

        public IList<OutboundMatchResult> GetMatches(VirtualPathContext context)
        {
            // Perf: Avoid allocation for List if there aren't any Matches or Criteria
            if (_root.Matches.Count > 0 || _root.Criteria.Count > 0)
            {
                var results = new List<OutboundMatchResult>();
                Walk(results, context, _root, isFallbackPath: false);
                results.Sort(OutboundMatchResultComparer.Instance);
                return results;
            }

            return null;            
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
            List<OutboundMatchResult> results,
            VirtualPathContext context,
            DecisionTreeNode<OutboundMatch> node,
            bool isFallbackPath)
        {
            // Any entries in node.Matches have had all their required values satisfied, so add them
            // to the results.
            for (var i = 0; i < node.Matches.Count; i++)
            {
                results.Add(new OutboundMatchResult(node.Matches[i], isFallbackPath));
            }

            for (var i = 0; i < node.Criteria.Count; i++)
            {
                var criterion = node.Criteria[i];
                var key = criterion.Key;

                object value;
                if (context.Values.TryGetValue(key, out value))
                {
                    DecisionTreeNode<OutboundMatch> branch;
                    if (criterion.Branches.TryGetValue(value ?? string.Empty, out branch))
                    {
                        Walk(results, context, branch, isFallbackPath);
                    }
                }
                else
                {
                    // If a value wasn't explicitly supplied, match BOTH the ambient value and the empty value
                    // if an ambient value was supplied. The path explored with the empty value is considered
                    // the fallback path.
                    DecisionTreeNode<OutboundMatch> branch;
                    if (context.AmbientValues.TryGetValue(key, out value) &&
                        !criterion.Branches.Comparer.Equals(value, string.Empty))
                    {
                        if (criterion.Branches.TryGetValue(value, out branch))
                        {
                            Walk(results, context, branch, isFallbackPath);
                        }
                    }

                    if (criterion.Branches.TryGetValue(string.Empty, out branch))
                    {
                        Walk(results, context, branch, isFallbackPath: true);
                    }
                }
            }
        }

        private class OutboundMatchClassifier : IClassifier<OutboundMatch>
        {
            public OutboundMatchClassifier()
            {
                ValueComparer = new RouteValueEqualityComparer();
            }

            public IEqualityComparer<object> ValueComparer { get; private set; }

            public IDictionary<string, DecisionCriterionValue> GetCriteria(OutboundMatch item)
            {
                var results = new Dictionary<string, DecisionCriterionValue>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in item.Entry.RequiredLinkValues)
                {
                    results.Add(kvp.Key, new DecisionCriterionValue(kvp.Value ?? string.Empty));
                }

                return results;
            }
        }

        private class OutboundMatchResultComparer : IComparer<OutboundMatchResult>
        {
            public static readonly OutboundMatchResultComparer Instance = new OutboundMatchResultComparer();

            public int Compare(OutboundMatchResult x, OutboundMatchResult y)
            {
                // For this comparison lower is better.
                if (x.Match.Entry.Order != y.Match.Entry.Order)
                {
                    return x.Match.Entry.Order.CompareTo(y.Match.Entry.Order);
                }

                if (x.Match.Entry.Precedence != y.Match.Entry.Precedence)
                {
                    // Reversed because higher is better
                    return y.Match.Entry.Precedence.CompareTo(x.Match.Entry.Precedence);
                }

                if (x.IsFallbackMatch != y.IsFallbackMatch)
                {
                    // A fallback match is worse than a non-fallback
                    return x.IsFallbackMatch.CompareTo(y.IsFallbackMatch);
                }

                return StringComparer.Ordinal.Compare(
                    x.Match.Entry.RouteTemplate.TemplateText, 
                    y.Match.Entry.RouteTemplate.TemplateText);
            }
        }
    }
}