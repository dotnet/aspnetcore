// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Routing.DecisionTree;

internal sealed class DecisionCriterion<TItem>
{
    public string Key { get; set; }

    public Dictionary<object, DecisionTreeNode<TItem>> Branches { get; set; }
}
