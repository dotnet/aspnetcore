// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Routing.Constraints
{
    /// <summary>
    /// Constraints a route parameter that must have a value.
    /// </summary>
    /// <remarks>
    /// This constraint is primarily used to enforce that a non-parameter value is present during
    /// URL generation.
    /// </remarks>
    public class RequiredRouteConstraint : IRouteConstraint
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
                // In routing the empty string is equivalent to null, which is equivalent to an unset value.
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return !string.IsNullOrEmpty(valueString);
            }

            return false;
        }
    }
}