// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Collections.Generic;

internal static class AsyncEnumerableExtensions
{
    public static async Task WaitForValueAsync<T>(this IAsyncEnumerator<T> values, T expectedValue) where T : INumber<T>
    {
        T value = T.Zero;
        try
        {
            while (await values.MoveNextAsync())
            {
                value = values.Current;
                if (value == expectedValue)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Data ended without match.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Results ended with final value of {value}. Expected value of {expectedValue}.", ex);
        }
    }

    public static async Task WaitForSumValueAsync<T>(this IAsyncEnumerator<T> values, T expectedValue) where T: INumber<T>
    {
        T value = T.Zero;
        try
        {
            while (await values.MoveNextAsync())
            {
                value += values.Current;
                if (value == expectedValue)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Data ended without match.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Results ended with final sum value of {value}. Expected sum value of {expectedValue}.", ex);
        }
    }
}
