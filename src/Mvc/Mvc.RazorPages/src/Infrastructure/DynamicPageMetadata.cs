// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DynamicPageMetadata : IDynamicEndpointMetadata
    {
        public DynamicPageMetadata(RouteValueDictionary values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Values = values;
        }

        public bool IsDynamic => true;

        public RouteValueDictionary Values { get; }
    }
}
