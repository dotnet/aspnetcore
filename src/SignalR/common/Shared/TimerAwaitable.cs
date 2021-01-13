// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Internal
{
    internal class TimerAwaitable : IDisposable, ICriticalNotifyCompletion
    {
        private Timer _timer;
        private Action _callback;
        private static readonly Action _callbackCompleted = () => { };

        private readonly TimeSpan _period;

        private readonly TimeSpan _dueTime;
        private bool _disposed;
        private bool _running = true;
        private object _lockObj = new object();

        public TimerAwaitable(TimeSpan dueTime, TimeSpan period)
        {
            _dueTime = dueTime;
            _period = period;
        }

        public void Start()
        {
            if (_timer == null)
            {
                lock (_lockObj)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    if (_timer == null)
                    {
                        // Don't capture the current ExecutionContext and its AsyncLocals onto the timer
                        bool restoreFlow = false;
                        try
                        {
                            if (!ExecutionContext.IsFlowSuppressed())
                            {
                                ExecutionContext.SuppressFlow();
                                restoreFlow = true;
                            }

                            _timer = new Timer(state => ((TimerAwaitable)state).Tick(), this, _dueTime, _period);
                        }
                        finally
                        {
                            // Restore the current ExecutionContext
                            if (restoreFlow)
                            {
                                ExecutionContext.RestoreFlow();
                            }
                        }
                    }
                }
            }
        }

        public TimerAwaitable GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public bool GetResult()
        {
            _callback = null;

            return _running;
        }

        private void Tick()
        {
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);
            continuation?.Invoke();
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Stop()
        {
            lock (_lockObj)
            {
                // Stop should be used to trigger the call to end the loop which disposes
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                _running = false;
            }

            // Call tick here to make sure that we yield the callback,
            // if it's currently waiting, we don't need to wait for the next period
            Tick();
        }

        void IDisposable.Dispose()
        {
            lock (_lockObj)
            {
                _disposed = true;

                _timer?.Dispose();

                _timer = null;
            }
        }
    }
}