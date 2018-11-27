// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// Behaves like a <see cref="TaskCompletionSource{T}"/>, but automatically times out
    /// the underlying task after a given period if not already completed.
    /// </summary>
    internal class AutoCancelTaskCompletionSource<T>
    {
        private readonly TaskCompletionSource<T> _completionSource;
        private readonly CancellationTokenSource _timeoutSource;

        public AutoCancelTaskCompletionSource(int timeoutMilliseconds)
        {
            _completionSource = new TaskCompletionSource<T>();
            _timeoutSource = new CancellationTokenSource();
            _timeoutSource.CancelAfter(timeoutMilliseconds);
            _timeoutSource.Token.Register(() => _completionSource.TrySetCanceled());
        }

        public Task Task => _completionSource.Task;

        public void TrySetResult(T result)
        {
            if (_completionSource.TrySetResult(result))
            {
                _timeoutSource.Dispose(); // We're not going to time out
            }
        }

        public void TrySetException(Exception exception)
        {
            if (_completionSource.TrySetException(exception))
            {
                _timeoutSource.Dispose(); // We're not going to time out
            }
        }
    }
}
