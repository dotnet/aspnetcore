// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public sealed class RouteValuesAddressMetadata : IRouteValuesAddressMetadata
    {
        public RouteValuesAddressMetadata(string routeName, IReadOnlyDictionary<string, object> requiredValues)
        {
            RouteName = routeName;
            RequiredValues = requiredValues;
        }

        public string RouteName { get; }

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