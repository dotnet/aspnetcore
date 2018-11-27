// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    internal class CurrentValues
    {
        public CurrentValues(ICollection<string> values)
        {
            Debug.Assert(values != null);
            Values = values;
        }

        public ICollection<string> Values { get; }

        public ICollection<string> ValuesAndEncodedValues { get; set; }
    }
}
