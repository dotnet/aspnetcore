// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public static class CancellationTokenExtensions
    {
        public static Task WaitForCancellationAsync(this CancellationToken token)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register((t) =>
            {
                ((TaskCompletionSource<object>)t).SetResult(null);
            }, tcs);
            return tcs.Task;
        }
    }
}
