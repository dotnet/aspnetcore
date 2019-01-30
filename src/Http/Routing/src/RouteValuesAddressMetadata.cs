// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Metadata used during link generation to find the associated endpoint using route values.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [Obsolete("Route values are now specified on a RoutePattern.")]
    public sealed class RouteValuesAddressMetadata : IRouteValuesAddressMetadata
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyRouteValues =
            new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        /// <summary>
        /// Creates a new instance of <see cref="RouteValuesAddressMetadata"/> with the provided route name.
        /// </summary>
        /// <param name="routeName">The route name. Can be null.</param>
        public RouteValuesAddressMetadata(string routeName) : this(routeName, EmptyRouteValues)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RouteValuesAddressMetadata"/> with the provided required route values.
        /// </summary>
        /// <param name="requiredValues">The required route values.</param>
        public RouteValuesAddressMetadata(IReadOnlyDictionary<string, object> requiredValues) : this(null, requiredValues)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RouteValuesAddressMetadata"/> with the provided route name and required route values.
        /// </summary>
        /// <param name="routeName">The route name. Can be null.</param>
        /// <param name="requiredValues">The required route values.</param>
        public RouteValuesAddressMetadata(string routeName, IReadOnlyDictionary<string, object> requiredValues)
        {
            if (requiredValues == null)
            {
                throw new ArgumentNullException(nameof(requiredValues));
            }

            RouteName = routeName;
            RequiredValues = requiredValues;
        }

        /// <summary>
        /// Gets the route name. Can be null.
        /// </summary>
        public string RouteName { get; }

        /// <summary>
        /// Gets the required route values.
        /// </summary>
        public IReadOnlyDictionary<string, object> RequiredValues { get; }

        internal string DebuggerToString()
        {
            return $"Name: {RouteName} - Required values: {string.Join(", ", FormatValues(RequiredValues))}";

            IEnumerable<string> FormatValues(IEnumerable<KeyValuePair<string, object>> values)
            {
                if (values == null)
                {
                    return Array.Empty<string>();
                }

                return values.Select(
                    kvp =>
                    {
                        var value = "null";
                        if (kvp.Value != null)
                        {
                            value = "\"" + kvp.Value.ToString() + "\"";
                        }
                        return kvp.Key + " = " + value;
                    });
            }
        }
    }
}