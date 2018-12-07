// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to represent only Boolean values.
    /// </summary>
    public class BoolRouteConstraint : IRouteConstraint
    {
        /// <inheritdoc />
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.TryGetValue(routeKey, out var value) && value != null)
            {
                if (value is bool)
                {
                    return true;
                }

                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return bool.TryParse(valueString, out _);
            }

            return false;
        }
    }
}