// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal abstract class RouteConstraint
    {
        // note: the things that prevent this cache from growing unbounded is that
        // we're the only caller to this code path, and the fact that there are only
        // 8 possible instances that we create.
        //
        // The values passed in here for parsing are always static text defined in route attributes.
        private static readonly ConcurrentDictionary<string, RouteConstraint> _cachedConstraints
            = new ConcurrentDictionary<string, RouteConstraint>();

        public abstract bool Match(string pathSegment, out object? convertedValue);

        public static RouteConstraint Parse(string template, string segment, string constraint)
        {
            if (string.IsNullOrEmpty(constraint))
            {
                throw new ArgumentException($"Malformed segment '{segment}' in route '{template}' contains an empty constraint.");
            }

            if (_cachedConstraints.TryGetValue(constraint, out var cachedInstance))
            {
                return cachedInstance;
            }
            else
            {
                var newInstance = CreateRouteConstraint(constraint);
                if (newInstance != null)
                {
                    // We've done to the work to create the constraint now, but it's possible
                    // we're competing with another thread. GetOrAdd can ensure only a single
                    // instance is returned so that any extra ones can be GC'ed.
                    return _cachedConstraints.GetOrAdd(constraint, newInstance);
                }
                else
                {
                    throw new ArgumentException($"Unsupported constraint '{constraint}' in route '{template}'.");
                }
            }
        }

        /// <summary>
        /// Creates a structured RouteConstraint object given a string that contains
        /// the route constraint. A constraint is the place after the colon in a
        /// parameter definition, for example `{age:int?}`.
        ///
        /// If the constraint denotes an optional, this method will return an
        /// <see cref="OptionalTypeRouteConstraint{T}" /> which handles the appropriate checks.
        /// </summary>
        /// <param name="constraint">String representation of the constraint</param>
        /// <returns>Type-specific RouteConstraint object</returns>
        private static RouteConstraint? CreateRouteConstraint(string constraint)
        {
            switch (constraint)
            {
                case "bool":
                    return new TypeRouteConstraint<bool>(bool.TryParse);
                case "bool?":
                    return new OptionalTypeRouteConstraint<bool>(bool.TryParse);
                case "datetime":
                    return new TypeRouteConstraint<DateTime>((string str, out DateTime result)
                        => DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result));
                case "datetime?":
                    return new OptionalTypeRouteConstraint<DateTime>((string str, out DateTime result)
                        => DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result));
                case "decimal":
                    return new TypeRouteConstraint<decimal>((string str, out decimal result)
                        => decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
                case "decimal?":
                    return new OptionalTypeRouteConstraint<decimal>((string str, out decimal result)
                        => decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
                case "double":
                    return new TypeRouteConstraint<double>((string str, out double result)
                        => double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
                case "double?":
                    return new OptionalTypeRouteConstraint<double>((string str, out double result)
                        => double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
                case "float":
                    return new TypeRouteConstraint<float>((string str, out float result)
                        => float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
                case "float?":
                    return new OptionalTypeRouteConstraint<float>((string str, out float result)
                        => float.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out result));
                case "guid":
                    return new TypeRouteConstraint<Guid>(Guid.TryParse);
                case "guid?":
                    return new OptionalTypeRouteConstraint<Guid>(Guid.TryParse);
                case "int":
                    return new TypeRouteConstraint<int>((string str, out int result)
                        => int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
                case "int?":
                    return new OptionalTypeRouteConstraint<int>((string str, out int result)
                        => int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
                case "long":
                    return new TypeRouteConstraint<long>((string str, out long result)
                        => long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
                case "long?":
                    return new OptionalTypeRouteConstraint<long>((string str, out long result)
                        => long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
                default:
                    return null;
            }
        }
    }
}
