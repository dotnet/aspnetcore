// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal
{
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
}
