// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to represent only 64-bit floating-point values.
    /// </summary>
    public class DoubleRouteConstraint : IRouteConstraint
    {
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
                if (value is double)
                {
                    return true;
                }

                double result;
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return Double.TryParse(valueString,
                                       NumberStyles.Float | NumberStyles.AllowThousands,
                                       CultureInfo.InvariantCulture,
                                       out result);
            }
            return false;
        }
    }
}