// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Routing.DecisionTree;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Tree;

// A decision tree that matches link generation entries based on route data.
[DebuggerDisplay("{DebuggerDisplayString,nq}")]
internal sealed class LinkGenerationDecisionTree
{
    // Fallback value for cases where the ambient values weren't provided.
    //
    // This is safe because we don't mutate the route values in here.
    private static readonly RouteValueDictionary EmptyAmbientValues = new RouteValueDictionary();

    private readonly DecisionTreeNode<OutboundMatch> _root;
    private readonly List<OutboundMatch> _conventionalEntries;

    public LinkGenerationDecisionTree(IReadOnlyList<OutboundMatch> entries)
    {
        // We split up the entries into:
        // 1. attribute routes - these go into the tree
        // 2. conventional routes - these are a list
        var attributedEntries = new List<OutboundMatch>();
        _conventionalEntries = new List<OutboundMatch>();

        // Anything with a RoutePattern.RequiredValueAny as a RequiredValue is a conventional route.
        // This is because RequiredValueAny acts as a wildcard, whereas an attribute route entry
        // is denormalized to contain an exact set of required values.
        //
        // We will only see conventional routes show up here for endpoint routing.
        for (var i = 0; i < entries.Count; i++)
        {
            var isAttributeRoute = true;
            var entry = entries[i];
            foreach (var kvp in entry.Entry.RequiredLinkValues)
            {
                if (RoutePattern.IsRequiredValueAny(kvp.Value))
                {
                    isAttributeRoute = false;
                    break;
                }
            }

            if (isAttributeRoute)
            {
                attributedEntries.Add(entry);
            }
            else
            {
                _conventionalEntries.Add(entry);
            }
        }

        _root = DecisionTreeBuilder<OutboundMatch>.GenerateTree(
            attributedEntries,
            new OutboundMatchClassifier());
    }

    public IList<OutboundMatchResult> GetMatches(RouteValueDictionary values, RouteValueDictionary ambientValues)
    {
        // Perf: Avoid allocation for List if there aren't any Matches or Criteria
        if (_root.Matches.Count > 0 || _root.Criteria.Count > 0 || _conventionalEntries.Count > 0)
        {
            var results = new List<OutboundMatchResult>();
            Walk(results, values, ambientValues ?? EmptyAmbientValues, _root, isFallbackPath: false);
            ProcessConventionalEntries(results);
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
    private static void Walk(
        List<OutboundMatchResult> results,
        RouteValueDictionary values,
        RouteValueDictionary ambientValues,
        DecisionTreeNode<OutboundMatch> node,
        bool isFallbackPath)
    {
        // Any entries in node.Matches have had all their required values satisfied, so add them
        // to the results.
        var matches = node.Matches;
        // Read interface .Count once rather than per iteration
        var matchesCount = matches.Count;
        for (var i = 0; i < matchesCount; i++)
        {
            results.Add(new OutboundMatchResult(matches[i], isFallbackPath));
        }

        var criteria = node.Criteria;
        // Read interface .Count once rather than per iteration
        var criteriaCount = criteria.Count;
        for (var i = 0; i < criteriaCount; i++)
        {
            var criterion = criteria[i];
            var key = criterion.Key;

            if (values.TryGetValue(key, out var value))
            {
                if (criterion.Branches.TryGetValue(value ?? string.Empty, out var branch))
                {
                    Walk(results, values, ambientValues, branch, isFallbackPath);
                }
            }
            else
            {
                // If a value wasn't explicitly supplied, match BOTH the ambient value and the empty value
                // if an ambient value was supplied. The path explored with the empty value is considered
                // the fallback path.
                DecisionTreeNode<OutboundMatch> branch;
                if (ambientValues.TryGetValue(key, out value) &&
                    !criterion.Branches.Comparer.Equals(value, string.Empty))
                {
                    if (criterion.Branches.TryGetValue(value, out branch))
                    {
                        Walk(results, values, ambientValues, branch, isFallbackPath);
                    }
                }

                if (criterion.Branches.TryGetValue(string.Empty, out branch))
                {
                    Walk(results, values, ambientValues, branch, isFallbackPath: true);
                }
            }
        }
    }

    private void ProcessConventionalEntries(List<OutboundMatchResult> results)
    {
        for (var i = 0; i < _conventionalEntries.Count; i++)
        {
            results.Add(new OutboundMatchResult(_conventionalEntries[i], isFallbackMatch: false));
        }
    }

    private sealed class OutboundMatchClassifier : IClassifier<OutboundMatch>
    {
        public IEqualityComparer<object> ValueComparer => RouteValueEqualityComparer.Default;

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

    private sealed class OutboundMatchResultComparer : IComparer<OutboundMatchResult>
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

            return string.Compare(
                x.Match.Entry.RouteTemplate.TemplateText,
                y.Match.Entry.RouteTemplate.TemplateText,
                StringComparison.Ordinal);
        }
    }

    // Example output:
    //
    // => action: Buy => controller: Store => version: V1(Matches: Store/Buy/V1)
    // => action: Buy => controller: Store => version: V2(Matches: Store/Buy/V2)
    // => action: Buy => controller: Store => area: Admin(Matches: Admin/Store/Buy)
    // => action: Buy => controller: Products(Matches: Products/Buy)
    // => action: Cart => controller: Store(Matches: Store/Cart)
    internal string DebuggerDisplayString
    {
        get
        {
            var sb = new StringBuilder();
            var branchStack = new Stack<string>();
            branchStack.Push(string.Empty);
            FlattenTree(branchStack, sb, _root);
            return sb.ToString();
        }
    }

    private static void FlattenTree(Stack<string> branchStack, StringBuilder sb, DecisionTreeNode<OutboundMatch> node)
    {
        // leaf node
        if (node.Criteria.Count == 0)
        {
            var matchesSb = new StringBuilder();
            foreach (var branch in branchStack)
            {
                matchesSb.Insert(0, branch);
            }
            sb.Append(matchesSb);
            sb.Append(" (Matches: ");
            sb.AppendJoin(", ", node.Matches.Select(m => m.Entry.RouteTemplate.TemplateText));
            sb.AppendLine(")");
        }

        foreach (var criterion in node.Criteria)
        {
            foreach (var branch in criterion.Branches)
            {
                branchStack.Push($" => {criterion.Key}: {branch.Key}");
                FlattenTree(branchStack, sb, branch.Value);
                branchStack.Pop();
            }
        }
    }
}
