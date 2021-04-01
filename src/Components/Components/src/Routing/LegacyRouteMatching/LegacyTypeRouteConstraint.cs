// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.LegacyRouteMatching
{
    /// <summary>
    /// A route constraint that requires the value to be parseable as a specified type.
    /// </summary>
    /// <typeparam name="T">The type to which the value must be parseable.</typeparam>
    internal class LegacyTypeRouteConstraint<T> : LegacyRouteConstraint
    {
        public delegate bool LegacyTryParseDelegate(string str, [MaybeNullWhen(false)] out T result);

        private readonly LegacyTryParseDelegate _parser;

        public LegacyTypeRouteConstraint(LegacyTryParseDelegate parser)
        {
            _parser = parser;
        }

        public override bool Match(string pathSegment, out object? convertedValue)
        {
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
