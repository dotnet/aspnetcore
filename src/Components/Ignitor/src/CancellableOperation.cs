// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Ignitor
{
    internal class CancellableOperation<TResult>
    {
        public CancellableOperation(TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Timeout = timeout;

            Completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            Completion.Task.ContinueWith(
                (task, state) =>
                {
                    var operation = (CancellableOperation<TResult>)state!;
                    operation.Dispose();
                },
                this,
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously, // We need to execute synchronously to clean-up before anything else continues
                TaskScheduler.Default);

            Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (Timeout != null && Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout != TimeSpan.MaxValue)
            {
                Cancellation.CancelAfter(Timeout.Value);
            }

            CancellationRegistration = Cancellation.Token.Register(
                (self) =>
                {
                    var operation = (CancellableOperation<TResult>)self!;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        // The operation was externally canceled before it timed out.
                        Dispose();
                        return;
                    }

                    operation.Completion.TrySetException(new TimeoutException($"The operation timed out after {Timeout}."));
                    operation.Cancellation?.Dispose();
                    operation.CancellationRegistration.Dispose();
                },
                this);
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
#nullable restore
