// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.LegacyRouteMatching
{
    /// <summary>
    /// A route constraint that allows the value to be null or parseable as the specified
    /// type.
    /// </summary>
    /// <typeparam name="T">The type to which the value must be parseable.</typeparam>
    internal class LegacyOptionalTypeRouteConstraint<T> : LegacyRouteConstraint
    {
        public delegate bool LegacyTryParseDelegate(string str, out T result);

        private readonly LegacyTryParseDelegate _parser;

        public LegacyOptionalTypeRouteConstraint(LegacyTryParseDelegate parser)
        {
            _parser = parser;
        }

        public override bool Match(string pathSegment, out object? convertedValue)
        {
            // Unset values are set to null in the Parameters object created in
            // the RouteContext. To match this pattern, unset optional parameters
            // are converted to null.
            if (string.IsNullOrEmpty(pathSegment))
            {
                convertedValue = null;
                return true;
            }

            if (_parser(pathSegment, out var result))
            {
                convertedValue = result;
                return true;
            }
            else
            {
                convertedValue = null;
                return false;
            }
        }
    }
}
