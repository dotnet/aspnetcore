// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal;

internal static class AwaitableThreadPool
{
    public static Awaitable Yield()
    {
        return new Awaitable();
    }

    public readonly struct Awaitable : ICriticalNotifyCompletion
    {
        public void GetResult()
        {
        }

        public Awaitable GetAwaiter() => this;

        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            Task.Run(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }
}
