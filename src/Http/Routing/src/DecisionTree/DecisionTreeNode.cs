// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Routing.DecisionTree;

// Data structure representing a node in a decision tree. These are created in DecisionTreeBuilder
// and walked to find a set of items matching some input criteria.
internal sealed class DecisionTreeNode<TItem>
{
    // The list of matches for the current node. This represents a set of items that have had all
    // of their criteria matched if control gets to this point in the tree.
    public IList<TItem> Matches { get; set; }

    // Additional criteria that further branch out from this node. Walk these to fine more items
    // matching the input data.
    public IList<DecisionCriterion<TItem>> Criteria { get; set; }
}
