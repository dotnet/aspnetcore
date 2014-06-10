// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to represent only <see cref="DateTime"/> values.
    /// Supports date time formats represented by CultureInfo.DateTimeFormat for the CultureInfo.InvariantCulture.
    /// </summary>
    public class DateTimeRouteConstraint : IRouteConstraint
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
                if (value is DateTime)
                {
                    return true;
                }

                DateTime result;
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return DateTime.TryParse(valueString, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            }

            return false;
        }
    }
}