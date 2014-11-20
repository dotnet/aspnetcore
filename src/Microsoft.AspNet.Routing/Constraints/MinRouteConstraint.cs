// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to be a long with a minimum value.
    /// </summary>
    public class MinRouteConstraint : IRouteConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MinRouteConstraint" /> class.
        /// </summary>
        /// <param name="min">The minimum value allowed for the route parameter.</param>
        public MinRouteConstraint(long min)
        {
            Min = min;
        }

        /// <summary>
        /// Gets the minimum allowed value of the route parameter.
        /// </summary>
        public long Min { get; private set; }

        /// <inheritdoc />
        public bool Match([NotNull] HttpContext httpContext,
                          [NotNull] IRouter route,
                          [NotNull] string routeKey,
                          [NotNull] IDictionary<string, object> values,
                          RouteDirection routeDirection)
        {
            object value;
            if (values.TryGetValue(routeKey, out value) && value != null)
            {
                long longValue;
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (Int64.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    return longValue >= Min;
                }
            }

            return false;
        }
    }
}