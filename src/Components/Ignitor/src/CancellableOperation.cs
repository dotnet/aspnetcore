// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ignitor
{
    internal class CancellableOperation<TResult>
    {
        public CancellableOperation(TimeSpan? timeout)
        {
            Timeout = timeout;

            Completion = new TaskCompletionSource<TResult>(TaskContinuationOptions.RunContinuationsAsynchronously);
            Completion.Task.ContinueWith(
                (task, state) =>
                {
                    var operation = (CancellableOperation<TResult>)state;
                    operation.Dispose();
                },
                this,
                TaskContinuationOptions.ExecuteSynchronously); // We need to execute synchronously to clean-up before anything else continues

            if (Timeout != null && Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout != TimeSpan.MaxValue)
            {
                Cancellation = new CancellationTokenSource(Timeout.Value);
                CancellationRegistration = Cancellation.Token.Register(
                    (self) =>
                    {
                        var operation = (CancellableOperation<TResult>)self;
                        operation.Completion.TrySetCanceled(operation.Cancellation.Token);
                        operation.Cancellation.Dispose();
                        operation.CancellationRegistration.Dispose();
                    },
                    this);
            }
        }

        public TimeSpan? Timeout { get; }

        public TaskCompletionSource<TResult> Completion { get; }

        public CancellationTokenSource Cancellation { get; }

        public CancellationTokenRegistration CancellationRegistration { get; }

        public bool Disposed { get; private set; }

        private void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            Completion.TrySetCanceled(Cancellation.Token);
            Cancellation.Dispose();
            CancellationRegistration.Dispose();
        }
    }
}
