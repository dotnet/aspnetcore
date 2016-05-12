// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    /// <summary>
    /// Summary description for KestrelThread
    /// </summary>
    public class KestrelThread
    {
        // maximum times the work queues swapped and are processed in a single pass
        // as completing a task may immediately have write data to put on the network
        // otherwise it needs to wait till the next pass of the libuv loop
        private const int _maxLoops = 8;

        private static readonly Action<object, object> _postCallbackAdapter = (callback, state) => ((Action<object>)callback).Invoke(state);
        private static readonly Action<object, object> _postAsyncCallbackAdapter = (callback, state) => ((Action<object>)callback).Invoke(state);

        private readonly KestrelEngine _engine;
        private readonly IApplicationLifetime _appLifetime;
        private readonly Thread _thread;
        private readonly UvLoopHandle _loop;
        private readonly UvAsyncHandle _post;
        private Queue<Work> _workAdding = new Queue<Work>(1024);
        private Queue<Work> _workRunning = new Queue<Work>(1024);
        private Queue<CloseHandle> _closeHandleAdding = new Queue<CloseHandle>(256);
        private Queue<CloseHandle> _closeHandleRunning = new Queue<CloseHandle>(256);
        private readonly object _workSync = new object();
        private readonly object _startSync = new object();
        private bool _stopImmediate = false;
        private bool _initCompleted = false;
        private ExceptionDispatchInfo _closeError;
        private readonly IKestrelTrace _log;
        private readonly IThreadPool _threadPool;

        public KestrelThread(KestrelEngine engine)
        {
            _engine = engine;
            _appLifetime = engine.AppLifetime;
            _log = engine.Log;
            _threadPool = engine.ThreadPool;
            _loop = new UvLoopHandle(_log);
            _post = new UvAsyncHandle(_log);
            _thread = new Thread(ThreadStart);
            _thread.Name = "KestrelThread - libuv";
#if !DEBUG
            // Mark the thread as being as unimportant to keeping the process alive.
            // Don't do this for debug builds, so we know if the thread isn't terminating.
            _thread.IsBackground = true;
#endif
            QueueCloseHandle = PostCloseHandle;
            QueueCloseAsyncHandle = EnqueueCloseHandle;
        }

        public UvLoopHandle Loop { get { return _loop; } }

        public ExceptionDispatchInfo FatalError { get { return _closeError; } }

        public Action<Action<IntPtr>, IntPtr> QueueCloseHandle { get; }

        private Action<Action<IntPtr>, IntPtr> QueueCloseAsyncHandle { get; }

        public Task StartAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            _thread.Start(tcs);
            return tcs.Task;
        }

        // This must be called from the libuv event loop.
        public void AllowStop()
        {
            _post.Unreference();
        }

        public void Stop(TimeSpan timeout)
        {
            lock (_startSync)
            {
                if (!_initCompleted)
                {
                    return;
                }
            }

            if (_thread.IsAlive)
            {
                var stepTimeout = (int)(timeout.TotalMilliseconds / 2);
                try
                {
                    Post(t => t.OnStopRude());
                    if (!_thread.Join(stepTimeout))
                    {
                        Post(t => t.OnStopImmediate());
                        if (!_thread.Join(stepTimeout))
                        {
                            _log.LogError(0, null, "KestrelThread.Stop failed to terminate libuv thread.");
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // REVIEW: Should we log something here?
                    // Until we rework this logic, ODEs are bound to happen sometimes.
                    if (!_thread.Join(stepTimeout))
                    {
                        _log.LogError(0, null, "KestrelThread.Stop failed to terminate libuv thread.");
                    }
                }
            }

            if (_closeError != null)
            {
                _closeError.Throw();
            }
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

            // uv_unref is idempotent so it's OK to call this here and in AllowStop.
            _post.Unreference();
        }

        private void OnStopImmediate()
        {
            _stopImmediate = true;
            _loop.Stop();
        }

        public void Post(Action<object> callback, object state)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _postCallbackAdapter,
                    Callback = callback,
                    State = state
                });
            }
            _post.Send();
        }

        private void Post(Action<KestrelThread> callback)
        {
            Post(thread => callback((KestrelThread)thread), this);
        }

        public Task PostAsync(Action<object> callback, object state)
        {
            var tcs = new TaskCompletionSource<object>();
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _postAsyncCallbackAdapter,
                    Callback = callback,
                    State = state,
                    Completion = tcs
                });
            }
            _post.Send();
            return tcs.Task;
        }

        public void Walk(Action<IntPtr> callback)
        {
            _engine.Libuv.walk(
                _loop,
                (ptr, arg) =>
                {
                    callback(ptr);
                },
                IntPtr.Zero);
        }

        private void PostCloseHandle(Action<IntPtr> callback, IntPtr handle)
        {
            EnqueueCloseHandle(callback, handle);
            _post.Send();
        }

        private void EnqueueCloseHandle(Action<IntPtr> callback, IntPtr handle)
        {
            lock (_workSync)
            {
                _closeHandleAdding.Enqueue(new CloseHandle { Callback = callback, Handle = handle });
            }
        }

        private void ThreadStart(object parameter)
        {
            lock (_startSync)
            {
                var tcs = (TaskCompletionSource<int>)parameter;
                try
                {
                    _loop.Init(_engine.Libuv);
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
                var ran1 = _loop.Run();
                if (_stopImmediate)
                {
                    // thread-abort form of exit, resources will be leaked
                    return;
                }

                // run the loop one more time to delete the open handles
                _post.Reference();
                _post.Dispose();

                // Ensure the Dispose operations complete in the event loop.
                var ran2 = _loop.Run();

                _loop.Dispose();
            }
            catch (Exception ex)
            {
                _closeError = ExceptionDispatchInfo.Capture(ex);
                // Request shutdown so we can rethrow this exception
                // in Stop which should be observable.
                _appLifetime.StopApplication();
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
                    if (work.Completion != null)
                    {
                        _threadPool.Complete(work.Completion);
                    }
                }
                catch (Exception ex)
                {
                    if (work.Completion != null)
                    {
                        _threadPool.Error(work.Completion, ex);
                    }
                    else
                    {
                        _log.LogError(0, ex, "KestrelThread.DoPostWork");
                        throw;
                    }
                }
            }

            return wasWork;
        }

        private bool DoPostCloseHandle()
        {
            Queue<CloseHandle> queue;
            lock (_workSync)
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
                    _log.LogError(0, ex, "KestrelThread.DoPostCloseHandle");
                    throw;
                }
            }

            return wasWork;
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
    }
}
