// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal class RouteContext
    {
        private static char[] Separator = new[] { '/' };

        public RouteContext(string path)
        {
            // This is a simplification. We are assuming there are no paths like /a//b/. A proper routing
            // implementation would be more sophisticated.
            Segments = path.Trim('/').Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] Segments { get; }

        public Type Handler { get; set; }

        public IDictionary<string, object> Parameters { get; set; }
    }
}