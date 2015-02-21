// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class FilterDescriptorOrderComparer : IComparer<FilterDescriptor>
    {
        private static readonly FilterDescriptorOrderComparer _comparer = new FilterDescriptorOrderComparer();

        public static FilterDescriptorOrderComparer Comparer
        {
            get { return _comparer; }
        }

        public int Compare([NotNull]FilterDescriptor x, [NotNull]FilterDescriptor y)
        {
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
