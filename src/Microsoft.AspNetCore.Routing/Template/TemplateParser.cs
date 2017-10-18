// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Dispatcher.Patterns;

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
                var inner = Microsoft.AspNetCore.Dispatcher.Patterns.RoutePattern.Parse(routeTemplate);
                return new RouteTemplate(inner);
            }
            catch (ArgumentException ex) when (ex.InnerException is RoutePatternException)
            {
                throw new ArgumentException(ex.InnerException.Message, nameof(routeTemplate), ex.InnerException);
            }
        }
    }
}
