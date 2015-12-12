// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to be an integer with a maximum value.
    /// </summary>
    public class MaxRouteConstraint : IRouteConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaxRouteConstraint" /> class.
        /// </summary>
        /// <param name="max">The maximum value allowed for the route parameter.</param>
        public MaxRouteConstraint(long max)
        {
            Max = max;
        }

        /// <summary>
        /// Gets the maximum allowed value of the route parameter.
        /// </summary>
        public long Max { get; private set; }

        /// <inheritdoc />
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            object value;
            if (values.TryGetValue(routeKey, out value) && value != null)
            {
                long longValue;
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (long.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    return longValue <= Max;
                }
            }

            return false;
        }
    }
}