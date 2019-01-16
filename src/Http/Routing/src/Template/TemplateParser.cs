// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    public static class TemplateParser
    {
        public static RouteTemplate Parse(string routeTemplate)
        {
            if (routeTemplate == null)
            {
                throw new ArgumentNullException(routeTemplate);
            }

            try
            {
                var inner = RoutePatternFactory.Parse(routeTemplate);
                return new RouteTemplate(inner);
            }
            catch (RoutePatternException ex)
            {
                // Preserving the existing behavior of this API even though the logic moved.
                throw new ArgumentException(ex.Message, nameof(routeTemplate), ex);
            }
        }
    }
}
