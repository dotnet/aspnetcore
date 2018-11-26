// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to be a string of a given length or within a given range of lengths.
    /// </summary>
    public class LengthRouteConstraint : IRouteConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LengthRouteConstraint" /> class that constrains
        /// a route parameter to be a string of a given length.
        /// </summary>
        /// <param name="length">The length of the route parameter.</param>
        public LengthRouteConstraint(int length)
        {
            if (length < 0)
            {
                var errorMessage = Resources.FormatArgumentMustBeGreaterThanOrEqualTo(0);
                throw new ArgumentOutOfRangeException(nameof(length), length, errorMessage);
            }

            MinLength = MaxLength = length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LengthRouteConstraint" /> class that constrains
        /// a route parameter to be a string of a given length.
        /// </summary>
        /// <param name="minLength">The minimum length allowed for the route parameter.</param>
        /// <param name="maxLength">The maximum length allowed for the route parameter.</param>
        public LengthRouteConstraint(int minLength, int maxLength)
        {
            if (minLength < 0)
            {
                var errorMessage = Resources.FormatArgumentMustBeGreaterThanOrEqualTo(0);
                throw new ArgumentOutOfRangeException(nameof(minLength), minLength, errorMessage);
            }

            if (maxLength < 0)
            {
                var errorMessage = Resources.FormatArgumentMustBeGreaterThanOrEqualTo(0);
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, errorMessage);
            }

            if (minLength > maxLength)
            {
                var errorMessage =
                    Resources.FormatRangeConstraint_MinShouldBeLessThanOrEqualToMax("minLength", "maxLength");
                throw new ArgumentOutOfRangeException(nameof(minLength), minLength, errorMessage);
            }

            MinLength = minLength;
            MaxLength = maxLength;
        }

        /// <summary>
        /// Gets the minimum length allowed for the route parameter.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// Gets the maximum length allowed for the route parameter.
        /// </summary>
        public int MaxLength { get; }

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
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                var length = valueString.Length;
                return length >= MinLength && length <= MaxLength;
            }

            return false;
        }
    }
}