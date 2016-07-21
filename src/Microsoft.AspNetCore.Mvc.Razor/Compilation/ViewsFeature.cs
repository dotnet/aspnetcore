// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public class ViewsFeature
    {
        public IDictionary<string, Type> Views { get; } =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    }
}
