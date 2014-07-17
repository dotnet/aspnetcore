// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.Routing.Logging
{
    internal static class StringBuilderHelpers
    {
        public static void Append<T>(StringBuilder builder, IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                builder.Append(Environment.NewLine);
                builder.Append("\t\t");
                builder.Append(item != null ? item.ToString() : "null");
            }
        }

        public static void Append<K, V>(StringBuilder builder, IDictionary<K, V> dict)
        {
            if (dict == null)
            {
                return;
            }

            foreach (var kvp in dict)
            {
                builder.Append(Environment.NewLine);
                builder.Append("\t\t");
                builder.Append(kvp.Key != null ? kvp.Key.ToString() : "null");
                builder.Append(" : ");
                builder.Append(kvp.Value != null ? kvp.Value.ToString() : "null");
            }
        }
    }
}