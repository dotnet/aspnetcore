// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.DecisionTree;

internal readonly struct DecisionCriterionValue
{
    private readonly object _value;

    public DecisionCriterionValue(object value)
    {
        _value = value;
    }

    public object Value
    {
        get { return _value; }
    }
}
