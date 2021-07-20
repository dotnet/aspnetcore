// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class FilterDescriptorOrderComparer : IComparer<FilterDescriptor>
    {
        public static FilterDescriptorOrderComparer Comparer { get; } = new FilterDescriptorOrderComparer();

        public int Compare(FilterDescriptor? x, FilterDescriptor? y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

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
}
