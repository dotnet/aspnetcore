// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class FilterItemOrderComparer : IComparer<FilterItem>
    {
        private static readonly FilterItemOrderComparer _comparer = new FilterItemOrderComparer();

        public static FilterItemOrderComparer Comparer
        {
            get { return _comparer; }
        }

        public int Compare(FilterItem x, FilterItem y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            return FilterDescriptorOrderComparer.Comparer.Compare(x.Descriptor, y.Descriptor);
        }
    }
}
