// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class LibuvAwaitable<TRequest> : ICriticalNotifyCompletion where TRequest : UvRequest
    {
        private readonly static Action _callbackCompleted = () => { };

        private Action _callback;

        private UvException _exception;

        private int _status;

        public static readonly Action<TRequest, int, UvException, object> Callback = (req, status, error, state) =>
        {
            var awaitable = (LibuvAwaitable<TRequest>)state;

            awaitable._exception = error;
            awaitable._status = status;

            var continuation = Interlocked.Exchange(ref awaitable._callback, _callbackCompleted);

            continuation?.Invoke();
        };

        public LibuvAwaitable<TRequest> GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public UvWriteResult GetResult()
        {
            Debug.Assert(_callback == _callbackCompleted);

            var exception = _exception;
            var status = _status;

            // Reset the awaitable state
            _exception = null;
            _status = 0;
            _callback = null;

            return new UvWriteResult(status, exception);
        }

        public void OnCompleted(Action continuation)
        {
            // There should never be a race between IsCompleted and OnCompleted since both operations
            // should always be on the libuv thread
            if (ReferenceEquals(_callback, _callbackCompleted))
            {
                Debug.Fail($"{typeof(LibuvAwaitable<TRequest>)}.{nameof(OnCompleted)} raced with {nameof(IsCompleted)}, scheduling callback.");
            }

            _callback = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }

    internal struct UvWriteResult
    {
        public int Status { get; }
        public UvException Error { get; }

        public UvWriteResult(int status, UvException error)
        {
            Status = status;
            Error = error;
        }
    }
}
