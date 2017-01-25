using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public static class AwaitableThreadPool
    {
        public static Awaitable Yield()
        {
            return new Awaitable();
        }

        public struct Awaitable : ICriticalNotifyCompletion
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
