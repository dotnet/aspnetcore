// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class SynchronousThreadPool : IThreadPool
    {
        public void Complete(TaskCompletionSource<object> tcs)
        {
            tcs.TrySetResult(null);
        }

        public void Cancel(TaskCompletionSource<object> tcs)
        {
            tcs.TrySetCanceled();
        }

        public void Error(TaskCompletionSource<object> tcs, Exception ex)
        {
            tcs.TrySetException(ex);
        }

        public void Run(Action action)
        {
            action();
        }

        public void UnsafeRun(WaitCallback action, object state)
        {
            action(state);
        }
    }
}
