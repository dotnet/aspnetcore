// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal class SocketAwaitableEventArgs : SocketAsyncEventArgs, IValueTaskSource<int>
    {
        private static readonly Action<object?> _continuationCompleted = _ => { };

        private readonly PipeScheduler _ioScheduler;

        private Action<object?>? _continuation;

        public SocketAwaitableEventArgs(PipeScheduler ioScheduler)
            : base(unsafeSuppressExecutionContextFlow: true)
        {
            _ioScheduler = ioScheduler;
        }

        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            var c = _continuation;

            if (c != null || (c = Interlocked.CompareExchange(ref _continuation, _continuationCompleted, null)) != null)
            {
                var continuationState = UserToken;
                UserToken = null;
                _continuation = _continuationCompleted; // in case someone's polling IsCompleted

                _ioScheduler.Schedule(c, continuationState);
            }
        }

        public int GetResult(short token)
        {
            _continuation = null;

            if (SocketError != SocketError.Success)
            {
                ThrowSocketException(SocketError);
            }

            return BytesTransferred;

            static void ThrowSocketException(SocketError e)
            {
                throw CreateException(e);
            }
        }

        protected static SocketException CreateException(SocketError e)
        {
            return new SocketException((int)e);
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return !ReferenceEquals(_continuation, _continuationCompleted) ? ValueTaskSourceStatus.Pending :
                    SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                    ValueTaskSourceStatus.Faulted;
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            UserToken = state;
            var prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (ReferenceEquals(prevContinuation, _continuationCompleted))
            {
                UserToken = null;
                ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
            }
        }
    }
}
