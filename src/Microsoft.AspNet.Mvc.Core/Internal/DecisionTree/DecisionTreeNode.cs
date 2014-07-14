// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Internal.DecisionTree
{
    public class DecisionTreeNode<TItem>
    {
        public List<TItem> Matches { get; set; }

        public List<DecisionCriterion<TItem>> Criteria { get; set; }
    }
}