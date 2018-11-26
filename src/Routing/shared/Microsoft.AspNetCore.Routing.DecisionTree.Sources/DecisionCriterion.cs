// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.DecisionTree
{
    internal class DecisionCriterion<TItem>
    {
        public string Key { get; set; }

        public Dictionary<object, DecisionTreeNode<TItem>> Branches { get; set; }
    }
}