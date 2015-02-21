// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    // Used to flow filters back from the FilterProviderContext
    [DebuggerDisplay("FilterItem: {Filter}")]
    public class FilterItem
    {
        public FilterItem([NotNull] FilterDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public FilterItem([NotNull] FilterDescriptor descriptor, [NotNull] IFilter filter)
            : this(descriptor)
        {
            Filter = filter;
        }

        public FilterDescriptor Descriptor { get; set; }

        public IFilter Filter { get; set; }
    }
}
