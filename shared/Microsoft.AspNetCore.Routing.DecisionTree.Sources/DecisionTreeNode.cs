// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.DecisionTree
{
    // Data structure representing a node in a decision tree. These are created in DecisionTreeBuilder
    // and walked to find a set of items matching some input criteria.
    internal class DecisionTreeNode<TItem>
    {
        // The list of matches for the current node. This represents a set of items that have had all
        // of their criteria matched if control gets to this point in the tree.
        public IList<TItem> Matches { get; set; }

        // Additional criteria that further branch out from this node. Walk these to fine more items
        // matching the input data.
        public IList<DecisionCriterion<TItem>> Criteria { get; set; }
    }
}