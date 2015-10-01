// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Mvc.Filters
{
    // Used to flow filters back from the FilterProviderContext
    [DebuggerDisplay("FilterItem: {Filter}")]
    public class FilterItem
    {
        public FilterItem(FilterDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Descriptor = descriptor;
        }

        public FilterItem(FilterDescriptor descriptor, IFilterMetadata filter)
            : this(descriptor)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            Filter = filter;
        }

        public FilterDescriptor Descriptor { get; set; }

        public IFilterMetadata Filter { get; set; }
    }
}
