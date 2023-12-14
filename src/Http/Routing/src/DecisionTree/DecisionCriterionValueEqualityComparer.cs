// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.DecisionTree;

internal sealed class DecisionCriterionValueEqualityComparer : IEqualityComparer<DecisionCriterionValue>
{
    public DecisionCriterionValueEqualityComparer(IEqualityComparer<object> innerComparer)
    {
        InnerComparer = innerComparer;
    }

    public IEqualityComparer<object> InnerComparer { get; private set; }

    public bool Equals(DecisionCriterionValue x, DecisionCriterionValue y)
    {
        return InnerComparer.Equals(x.Value, y.Value);
    }

    public int GetHashCode(DecisionCriterionValue obj)
    {
        return InnerComparer.GetHashCode(obj.Value);
    }
}
