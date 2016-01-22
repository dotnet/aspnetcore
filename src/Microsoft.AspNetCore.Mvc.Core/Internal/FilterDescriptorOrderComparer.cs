// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class FilterDescriptorOrderComparer : IComparer<FilterDescriptor>
    {
        private static readonly FilterDescriptorOrderComparer _comparer = new FilterDescriptorOrderComparer();

        public static FilterDescriptorOrderComparer Comparer
        {
            get { return _comparer; }
        }

        public int Compare(FilterDescriptor x, FilterDescriptor y)
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
