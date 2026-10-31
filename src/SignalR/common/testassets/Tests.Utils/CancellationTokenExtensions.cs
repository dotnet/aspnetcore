// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Tests;

public static class CancellationTokenExtensions
{
    public static Task WaitForCancellationAsync(this CancellationToken token)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        token.Register((t) =>
        {
            ((TaskCompletionSource)t).SetResult();
        }, tcs);
        return tcs.Task;
    }
}
