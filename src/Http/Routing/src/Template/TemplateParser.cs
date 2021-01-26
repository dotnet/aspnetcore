// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    /// <summary>
    /// Provides methods for parsing route template strings.
    /// </summary>
    public static class TemplateParser
    {
        /// <summary>
        /// Creates a <see cref="RouteTemplate"/> for a given <paramref name="routeTemplate"/> string.
        /// </summary>
        /// <param name="routeTemplate">A string representation of the route template.</param>
        /// <returns>A <see cref="RouteTemplate"/> instance.</returns>
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
