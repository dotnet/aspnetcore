// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal readonly struct ConventionalRouteEntry
    {
        public readonly RoutePattern Pattern;
        public readonly string RouteName;
        public readonly RouteValueDictionary DataTokens;

        public ConventionalRouteEntry(
            string routeName,
            string pattern,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints,
            RouteValueDictionary dataTokens)
        {
            RouteName = routeName;
            DataTokens = dataTokens;

            try
            {
                // Data we parse from the pattern will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                Pattern = RoutePatternFactory.Parse(pattern, defaults, constraints);
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(string.Format(
                    CultureInfo.CurrentCulture, 
                    "An error occurred while creating the route with name '{0}' and pattern '{1}'.", 
                    routeName, 
                    pattern), exception);
            }
        }

        public ConventionalRouteEntry(RoutePattern pattern, string routeName, RouteValueDictionary dataTokens)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            Pattern = pattern;
            RouteName = routeName;
            DataTokens = dataTokens;
        }
    }
}
