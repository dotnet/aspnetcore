// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    public class RegexRouteConstraint : IRouteConstraint
    {
        public RegexRouteConstraint([NotNull] Regex regex)
        {
            Constraint = regex;
        }

        public RegexRouteConstraint([NotNull] string regexPattern)
        {
            Constraint = new Regex(regexPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        public Regex Constraint { get; private set; }

        public bool Match([NotNull] HttpContext httpContext,
                          [NotNull] IRouter route,
                          [NotNull] string routeKey,
                          [NotNull] IDictionary<string, object> routeValues,
                          RouteDirection routeDirection)
        {
            object routeValue;

            if (routeValues.TryGetValue(routeKey, out routeValue)
                && routeValue != null)
            {
                var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);

                return Constraint.IsMatch(parameterValueString);
            }

            return false;
        }
    }
}
