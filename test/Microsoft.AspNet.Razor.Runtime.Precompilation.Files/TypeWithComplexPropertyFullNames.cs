// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public class TypeWithComplexPropertyFullNames
    {
        public int Property1 { get; set; }

        public int[] Property2 { get; set; }

        public List<long> Property3 { get; set; }

        public List<Tuple<string, DateTimeOffset>> Property4 { get; }

        public IDictionary<ILookup<string, TagHelper>, IList<Comparer<byte[]>>> Property5 { get; }
    }
}
