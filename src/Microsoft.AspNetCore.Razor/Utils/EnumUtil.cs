// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Utils
{
    internal static class EnumUtil
    {
        public static IEnumerable<T> Single<T>(T item)
        {
            yield return item;
        }

        public static IEnumerable<T> Prepend<T>(T item, IEnumerable<T> enumerable)
        {
            yield return item;
            foreach (T t in enumerable)
            {
                yield return t;
            }
        }
    }
}
