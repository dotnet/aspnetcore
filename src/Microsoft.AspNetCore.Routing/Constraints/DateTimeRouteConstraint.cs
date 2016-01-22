// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to represent only <see cref="DateTime"/> values.
    /// </summary>
    /// <remarks>
    /// This constraint tries to parse strings by using all of the formats returned by the
    /// CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns() method.
    /// For a sample on how to list all formats which are considered, please visit
    /// http://msdn.microsoft.com/en-us/library/aszyst2c(v=vs.110).aspx
    /// </remarks>
    public class DateTimeRouteConstraint : IRouteConstraint
    {
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