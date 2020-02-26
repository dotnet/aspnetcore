// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    internal static class AsyncEnumerableExtensions
    {
        public static async Task<T> FirstOrDefault<T>(this IAsyncEnumerator<T> values, Func<T, bool> filter)
        {
            while (await values.MoveNextAsync())
            {
                if (filter(values.Current))
                {
                    return values.Current;
                }
            }

            return default;
        }
    }
}
