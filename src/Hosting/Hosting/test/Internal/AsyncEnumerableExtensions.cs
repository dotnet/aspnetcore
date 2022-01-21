// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic;

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
