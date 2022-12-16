// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class ThreadPoolAwaitable : ICriticalNotifyCompletion
{
    public static readonly ThreadPoolAwaitable Instance = new ThreadPoolAwaitable();

    private ThreadPoolAwaitable()
    {
    }

    public ThreadPoolAwaitable GetAwaiter() => this;
    public bool IsCompleted => false;

    public void GetResult()
    {
    }

    public void OnCompleted(Action continuation)
    {
        ThreadPool.UnsafeQueueUserWorkItem(state => ((Action)state!)(), continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }
}
