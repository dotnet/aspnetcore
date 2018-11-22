// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RoutingWebSite
{
    internal class EndsWithStringRouteConstraint : IRouteConstraint
    {
        private readonly string _endsWith;

        public EndsWithStringRouteConstraint(string endsWith)
        {
            _endsWith = endsWith;
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var value = values[routeKey];
            if (value == null)
            {
                return false;
            }

            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            var endsWith = valueString.EndsWith(_endsWith, StringComparison.OrdinalIgnoreCase);
            return endsWith;
        }
    }
}
