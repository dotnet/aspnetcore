// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    internal static class IEnumerableExtensions
    {
        public static T[] AsArray<T>(this IEnumerable<T> values)
        {
            Debug.Assert(values != null);

            var array = values as T[];
            if (array == null)
            {
                array = values.ToArray();
            }
            return array;
        }
    }
}
