// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    internal static class AwaitableThreadPool
    {
        internal static Awaitable Yield()
        {
            return new Awaitable();
        }

        internal struct Awaitable : ICriticalNotifyCompletion
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
}