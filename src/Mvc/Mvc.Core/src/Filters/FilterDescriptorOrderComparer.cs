// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class FilterDescriptorOrderComparer : IComparer<FilterDescriptor>
{
    public static FilterDescriptorOrderComparer Comparer { get; } = new FilterDescriptorOrderComparer();

    public int Compare(FilterDescriptor? x, FilterDescriptor? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        if (x.Order == y.Order)
        {
            return x.Scope.CompareTo(y.Scope);
        }
        else
        {
            return x.Order.CompareTo(y.Order);
        }
    }
}
