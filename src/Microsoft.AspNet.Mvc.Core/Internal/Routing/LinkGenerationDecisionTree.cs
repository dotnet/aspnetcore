// Copyright (c) .NET Foundation. All rights reserved.
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

        public IList<LinkGenerationMatch> GetMatches(VirtualPathContext context)
        {
            var results = new List<LinkGenerationMatch>();
            Walk(results, context, _root, isFallbackPath: false);
            results.Sort(LinkGenerationMatchComparer.Instance);
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
            List<LinkGenerationMatch> results,
            VirtualPathContext context,
            DecisionTreeNode<AttributeRouteLinkGenerationEntry> node,
            bool isFallbackPath)
        {
            // Any entries in node.Matches have had all their required values satisfied, so add them
            // to the results.
            for (var i = 0; i < node.Matches.Count; i++)
            {
                results.Add(new LinkGenerationMatch(node.Matches[i], isFallbackPath));
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
                        Walk(results, context, branch, isFallbackPath);
                    }
                }
                else
                {
                    // If a value wasn't explicitly supplied, match BOTH the ambient value and the empty value
                    // if an ambient value was supplied. The path explored with the empty value is considered
                    // the fallback path.
                    DecisionTreeNode<AttributeRouteLinkGenerationEntry> branch;
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

        private class LinkGenerationMatchComparer : IComparer<LinkGenerationMatch>
        {
            public static readonly LinkGenerationMatchComparer Instance = new LinkGenerationMatchComparer();

            public int Compare(LinkGenerationMatch x, LinkGenerationMatch y)
            {
                // For this comparison lower is better.
                if (x.Entry.Order != y.Entry.Order)
                {
                    return x.Entry.Order.CompareTo(y.Entry.Order);
                }

                if (x.Entry.GenerationPrecedence != y.Entry.GenerationPrecedence)
                {
                    // Reversed because higher is better
                    return y.Entry.GenerationPrecedence.CompareTo(x.Entry.GenerationPrecedence);
                }

                if (x.IsFallbackMatch != y.IsFallbackMatch)
                {
                    // A fallback match is worse than a non-fallback
                    return x.IsFallbackMatch.CompareTo(y.IsFallbackMatch);
                }

                return StringComparer.Ordinal.Compare(x.Entry.TemplateText, y.Entry.TemplateText);
            }
        }
    }
}