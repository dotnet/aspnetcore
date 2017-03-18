// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public class LibuvAwaitable<TRequest> : ICriticalNotifyCompletion where TRequest : UvRequest
    {
        private readonly static Action _callbackCompleted = () => { };

        private Action _callback;

        private Exception _exception;

        private int _status;

        public static readonly Action<TRequest, int, Exception, object> Callback = (req, status, error, state) =>
        {
            var awaitable = (LibuvAwaitable<TRequest>)state;

            awaitable._exception = error;
            awaitable._status = status;

            var continuation = Interlocked.Exchange(ref awaitable._callback, _callbackCompleted);

            continuation?.Invoke();
        };

        public LibuvAwaitable<TRequest> GetAwaiter() => this;
        public bool IsCompleted => _callback == _callbackCompleted;

        public UvWriteResult GetResult()
        {
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
            if (_callback == _callbackCompleted ||
                Interlocked.CompareExchange(ref _callback, continuation, null) == _callbackCompleted)
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }

    public struct UvWriteResult
    {
        public int Status { get; }
        public Exception Error { get; }

        public UvWriteResult(int status, Exception error)
        {
            Status = status;
            Error = error;
        }
    }
}