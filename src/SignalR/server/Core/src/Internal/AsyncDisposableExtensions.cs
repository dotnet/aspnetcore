// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class AsyncDisposableExtensions
{
    // Does a light up check to see if a type is IAsyncDisposable and calls DisposeAsync if it is
    public static ValueTask DisposeAsync(this IDisposable disposable)
    {
        if (disposable is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }
        disposable.Dispose();
        return default;
    }
}
