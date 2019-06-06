// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class LibuvThread : PipeScheduler
    {
        // maximum times the work queues swapped and are processed in a single pass
        // as completing a task may immediately have write data to put on the network
        // otherwise it needs to wait till the next pass of the libuv loop
        private readonly int _maxLoops;

        private readonly LibuvFunctions _libuv;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly Thread _thread;
        private readonly TaskCompletionSource<object> _threadTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly UvLoopHandle _loop;
        private readonly UvAsyncHandle _post;
        private Queue<Work> _workAdding = new Queue<Work>(1024);
        private Queue<Work> _workRunning = new Queue<Work>(1024);
        private Queue<CloseHandle> _closeHandleAdding = new Queue<CloseHandle>(256);
        private Queue<CloseHandle> _closeHandleRunning = new Queue<CloseHandle>(256);
        private readonly object _workSync = new object();
        private readonly object _closeHandleSync = new object();
        private readonly object _startSync = new object();
        private bool _stopImmediate = false;
        private bool _initCompleted = false;
        private Exception _closeError;
        private readonly ILibuvTrace _log;

        public LibuvThread(LibuvFunctions libuv, LibuvTransportContext libuvTransportContext, int maxLoops = 8)
            : this(libuv, libuvTransportContext.AppLifetime, libuvTransportContext.Options.MemoryPoolFactory(), libuvTransportContext.Log, maxLoops)
        {
        }

        public LibuvThread(LibuvFunctions libuv, IHostApplicationLifetime appLifetime, MemoryPool<byte> pool, ILibuvTrace log, int maxLoops = 8)
        {
            _libuv = libuv;
            _appLifetime = appLifetime;
            _log = log;
            _loop = new UvLoopHandle(_log);
            _post = new UvAsyncHandle(_log);
            _maxLoops = maxLoops;

            _thread = new Thread(ThreadStart);
#if !INNER_LOOP
            _thread.Name = nameof(LibuvThread);
#endif

#if !DEBUG
            // Mark the thread as being as unimportant to keeping the process alive.
            // Don't do this for debug builds, so we know if the thread isn't terminating.
            _thread.IsBackground = true;
#endif
            QueueCloseHandle = PostCloseHandle;
            QueueCloseAsyncHandle = EnqueueCloseHandle;
            MemoryPool = pool;
            WriteReqPool = new WriteReqPool(this, _log);
        }

        public UvLoopHandle Loop { get { return _loop; } }

        public MemoryPool<byte> MemoryPool { get; }

        public WriteReqPool WriteReqPool { get; }

#if DEBUG
        public List<WeakReference> Requests { get; } = new List<WeakReference>();
#endif

        public Exception FatalError => _closeError;

        public Action<Action<IntPtr>, IntPtr> QueueCloseHandle { get; }

        private Action<Action<IntPtr>, IntPtr> QueueCloseAsyncHandle { get; }

        public Task StartAsync()
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _thread.Start(tcs);
            return tcs.Task;
        }

        public async Task StopAsync(TimeSpan timeout)
        {
            lock (_startSync)
            {
                if (!_initCompleted)
                {
                    return;
                }
            }

            Debug.Assert(!_threadTcs.Task.IsCompleted, "The loop thread was completed before calling uv_unref on the post handle.");

            var stepTimeout = TimeSpan.FromTicks(timeout.Ticks / 3);

            try
            {
                Post(t => t.AllowStop());
                if (!await WaitAsync(_threadTcs.Task, stepTimeout).ConfigureAwait(false))
                {
                    _log.LogWarning($"{nameof(LibuvThread)}.{nameof(StopAsync)} failed to terminate libuv thread, {nameof(AllowStop)}");

                    Post(t => t.OnStopRude());
                    if (!await WaitAsync(_threadTcs.Task, stepTimeout).ConfigureAwait(false))
                    {
                        _log.LogCritical($"{nameof(LibuvThread)}.{nameof(StopAsync)} failed to terminate libuv thread, {nameof(OnStopRude)}.");

                        Post(t => t.OnStopImmediate());
                        if (!await WaitAsync(_threadTcs.Task, stepTimeout).ConfigureAwait(false))
                        {
                            _log.LogCritical($"{nameof(LibuvThread)}.{nameof(StopAsync)} failed to terminate libuv thread, {nameof(OnStopImmediate)}.");
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                if (!await WaitAsync(_threadTcs.Task, stepTimeout).ConfigureAwait(false))
                {
                    _log.LogCritical($"{nameof(LibuvThread)}.{nameof(StopAsync)} failed to terminate libuv thread.");
                }
            }

            if (_closeError != null)
            {
                ExceptionDispatchInfo.Capture(_closeError).Throw();
            }
        }

#if DEBUG && !INNER_LOOP
        private void CheckUvReqLeaks()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Detect leaks in UvRequest objects
            foreach (var request in Requests)
            {
                Debug.Assert(request.Target == null, $"{request.Target?.GetType()} object is still alive.");
            }
        }
#endif

        private void AllowStop()
        {
            _post.Unreference();
        }

        private void OnStopRude()
        {
            Walk(ptr =>
            {
                var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                if (handle != _post)
                {
                    // handle can be null because UvMemory.FromIntPtr looks up a weak reference
                    handle?.Dispose();
                }
            });
        }

        private void OnStopImmediate()
        {
            _stopImmediate = true;
            _loop.Stop();
        }

        public void Post<T>(Action<T> callback, T state)
        {
            // Handle is closed to don't bother scheduling anything
            if (_post.IsClosed)
            {
                return;
            }

            var work = new Work
            {
                CallbackAdapter = CallbackAdapter<T>.PostCallbackAdapter,
                Callback = callback,
                // TODO: This boxes
                State = state
            };

            lock (_workSync)
            {
                _workAdding.Enqueue(work);
            }

            try
            {
                _post.Send();
            }
            catch (ObjectDisposedException)
            {
                // There's an inherent race here where we're in the middle of shutdown
            }
        }

        private void Post(Action<LibuvThread> callback)
        {
            Post(callback, this);
        }

        public Task PostAsync<T>(Action<T> callback, T state)
        {
            // Handle is closed to don't bother scheduling anything
            if (_post.IsClosed)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var work = new Work
            {
                CallbackAdapter = CallbackAdapter<T>.PostAsyncCallbackAdapter,
                Callback = callback,
                State = state,
                Completion = tcs
            };

            lock (_workSync)
            {
                _workAdding.Enqueue(work);
            }

            try
            {
                _post.Send();
            }
            catch (ObjectDisposedException)
            {
                // There's an inherent race here where we're in the middle of shutdown
            }
            return tcs.Task;
        }

        public void Walk(Action<IntPtr> callback)
        {
            Walk((ptr, arg) => callback(ptr), IntPtr.Zero);
        }

        private void Walk(LibuvFunctions.uv_walk_cb callback, IntPtr arg)
        {
            _libuv.walk(
                _loop,
                callback,
                arg
                );
        }

        private void PostCloseHandle(Action<IntPtr> callback, IntPtr handle)
        {
            EnqueueCloseHandle(callback, handle);
            _post.Send();
        }

        private void EnqueueCloseHandle(Action<IntPtr> callback, IntPtr handle)
        {
            var closeHandle = new CloseHandle { Callback = callback, Handle = handle };
            lock (_closeHandleSync)
            {
                _closeHandleAdding.Enqueue(closeHandle);
            }
        }

        private void ThreadStart(object parameter)
        {
            lock (_startSync)
            {
                var tcs = (TaskCompletionSource<int>)parameter;
                try
                {
                    _loop.Init(_libuv);
                    _post.Init(_loop, OnPost, EnqueueCloseHandle);
                    _initCompleted = true;
                    tcs.SetResult(0);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    return;
                }
            }

            try
            {
                _loop.Run();
                if (_stopImmediate)
                {
                    // thread-abort form of exit, resources will be leaked
                    return;
                }

                // run the loop one more time to delete the open handles
                _post.Reference();
                _post.Dispose();

                // We need this walk because we call ReadStop on accepted connections when there's back pressure
                // Calling ReadStop makes the handle as in-active which means the loop can
                // end while there's still valid handles around. This makes loop.Dispose throw
                // with an EBUSY. To avoid that, we walk all of the handles and dispose them.
                Walk(ptr =>
                {
                    var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                    // handle can be null because UvMemory.FromIntPtr looks up a weak reference
                    handle?.Dispose();
                });

                // Ensure the Dispose operations complete in the event loop.
                _loop.Run();

                _loop.Dispose();
            }
            catch (Exception ex)
            {
                _closeError = ex;
                // Request shutdown so we can rethrow this exception
                // in Stop which should be observable.
                _appLifetime.StopApplication();
            }
            finally
            {
                try
                {
                    MemoryPool.Dispose();
                }
                catch (Exception ex)
                {
                    _closeError = _closeError == null ? ex : new AggregateException(_closeError, ex);
                }
                WriteReqPool.Dispose();
                _threadTcs.SetResult(null);

#if DEBUG && !INNER_LOOP
                // Check for handle leaks after disposing everything
                CheckUvReqLeaks();
#endif
            }
        }

        private void OnPost()
        {
            var loopsRemaining = _maxLoops;
            bool wasWork;
            do
            {
                wasWork = DoPostWork();
                wasWork = DoPostCloseHandle() || wasWork;
                loopsRemaining--;
            } while (wasWork && loopsRemaining > 0);
        }

        private bool DoPostWork()
        {
            Queue<Work> queue;
            lock (_workSync)
            {
                queue = _workAdding;
                _workAdding = _workRunning;
                _workRunning = queue;
            }

            bool wasWork = queue.Count > 0;

            while (queue.Count != 0)
            {
                var work = queue.Dequeue();
                try
                {
                    work.CallbackAdapter(work.Callback, work.State);
                    work.Completion?.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    if (work.Completion != null)
                    {
                        work.Completion.TrySetException(ex);
                    }
                    else
                    {
                        _log.LogError(0, ex, $"{nameof(LibuvThread)}.{nameof(DoPostWork)}");
                        throw;
                    }
                }
            }

            return wasWork;
        }

        private bool DoPostCloseHandle()
        {
            Queue<CloseHandle> queue;
            lock (_closeHandleSync)
            {
                queue = _closeHandleAdding;
                _closeHandleAdding = _closeHandleRunning;
                _closeHandleRunning = queue;
            }

            bool wasWork = queue.Count > 0;

            while (queue.Count != 0)
            {
                var closeHandle = queue.Dequeue();
                try
                {
                    closeHandle.Callback(closeHandle.Handle);
                }
                catch (Exception ex)
                {
                    _log.LogError(0, ex, $"{nameof(LibuvThread)}.{nameof(DoPostCloseHandle)}");
                    throw;
                }
            }

            return wasWork;
        }

        private static async Task<bool> WaitAsync(Task task, TimeSpan timeout)
        {
            return await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false) == task;
        }

        public override void Schedule(Action<object> action, object state)
        {
            Post(action, state);
        }

        private struct Work
        {
            public Action<object, object> CallbackAdapter;
            public object Callback;
            public object State;
            public TaskCompletionSource<object> Completion;
        }

        private struct CloseHandle
        {
            public Action<IntPtr> Callback;
            public IntPtr Handle;
        }

        private class CallbackAdapter<T>
        {
            public static readonly Action<object, object> PostCallbackAdapter = (callback, state) => ((Action<T>)callback).Invoke((T)state);
            public static readonly Action<object, object> PostAsyncCallbackAdapter = (callback, state) => ((Action<T>)callback).Invoke((T)state);
        }
    }
}
