// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.InternalTesting;

internal static class AsyncEnumerableExtensions
{
    public static async Task WaitForValueAsync<T>(this IAsyncEnumerator<T> values, T expectedValue, string operationName, ILogger logger) where T : INumber<T>
    {
        T value = T.Zero;
        try
        {
            while (await values.MoveNextAsync())
            {
                value = values.Current;
                if (value == expectedValue)
                {
                    logger.LogDebug("Operation {OperationName} completed with value {Value}.", operationName, value);
                    return;
                }

                logger.LogDebug("Operation {OperationName} expected {ExpectedValue} but got {Value}.", operationName, expectedValue, value);
            }

            throw new InvalidOperationException("Data ended without match.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Results ended with final value of {value}. Expected value of {expectedValue}.", ex);
        }
    }
}
