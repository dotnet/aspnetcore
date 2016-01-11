// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel
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

        private static readonly Action<object, object> _threadCallbackAdapter = (callback, state) => ((Action<KestrelThread>)callback).Invoke((KestrelThread)state);
        private static readonly Action<object, object> _socketCallbackAdapter = (callback, state) => ((Action<SocketOutput>)callback).Invoke((SocketOutput)state);
        private static readonly Action<object, object> _tcsCallbackAdapter = (callback, state) => ((Action<TaskCompletionSource<int>>)callback).Invoke((TaskCompletionSource<int>)state);
        private static readonly Action<object, object> _listenerPrimaryCallbackAdapter = (callback, state) => ((Action<ListenerPrimary>)callback).Invoke((ListenerPrimary)state);
        private static readonly Action<object, object> _listenerSecondaryCallbackAdapter = (callback, state) => ((Action<ListenerSecondary>)callback).Invoke((ListenerSecondary)state);

        private readonly KestrelEngine _engine;
        private readonly IApplicationLifetime _appLifetime;
        private readonly Thread _thread;
        private readonly UvLoopHandle _loop;
        private readonly UvAsyncHandle _post;
        private Queue<Work> _workAdding = new Queue<Work>(1024);
        private Queue<Work> _workRunning = new Queue<Work>(1024);
        private Queue<CloseHandle> _closeHandleAdding = new Queue<CloseHandle>(256);
        private Queue<CloseHandle> _closeHandleRunning = new Queue<CloseHandle>(256);
        private readonly object _workSync = new Object();
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
            QueueCloseHandle = PostCloseHandle;
        }

        public UvLoopHandle Loop { get { return _loop; } }
        public ExceptionDispatchInfo FatalError { get { return _closeError; } }

        public Action<Action<IntPtr>, IntPtr> QueueCloseHandle { get; internal set; }

        public Task StartAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            _thread.Start(tcs);
            return tcs.Task;
        }

        public void Stop(TimeSpan timeout)
        {
            if (!_initCompleted)
            {
                return;
            }

            var stepTimeout = (int)(timeout.TotalMilliseconds / 3); 

            Post(t => t.OnStop());
            if (!_thread.Join(stepTimeout))
            {
                try
                {
                    Post(t => t.OnStopRude());
                    if (!_thread.Join(stepTimeout))
                    {
                        Post(t => t.OnStopImmediate());
                        if (!_thread.Join(stepTimeout))
                        {
#if NET451
                            _thread.Abort();
#endif
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // REVIEW: Should we log something here?
                    // Until we rework this logic, ODEs are bound to happen sometimes.
                    if (!_thread.Join(stepTimeout))
                    {
#if NET451
                        _thread.Abort();
#endif
                    }
                }
            }

            if (_closeError != null)
            {
                _closeError.Throw();
            }
        }

        private void OnStop()
        {
            _post.Unreference();
        }

        private void OnStopRude()
        {
            _engine.Libuv.walk(
                _loop,
                (ptr, arg) =>
                {
                    var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                    if (handle != _post)
                    {
                        handle.Dispose();
                    }
                },
                IntPtr.Zero);
        }

        private void OnStopImmediate()
        {
            _stopImmediate = true;
            _loop.Stop();
        }

        private void Post(Action<KestrelThread> callback)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work { CallbackAdapter = _threadCallbackAdapter, Callback = callback, State = this });
            }
            _post.Send();
        }

        public void Post(Action<SocketOutput> callback, SocketOutput state)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _socketCallbackAdapter,
                    Callback = callback,
                    State = state
                });
            }
            _post.Send();
        }

        public void Post(Action<TaskCompletionSource<int>> callback, TaskCompletionSource<int> state)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _tcsCallbackAdapter,
                    Callback = callback,
                    State = state
                });
            }
            _post.Send();
        }

        public Task PostAsync(Action<ListenerPrimary> callback, ListenerPrimary state)
        {
            var tcs = new TaskCompletionSource<object>();
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _listenerPrimaryCallbackAdapter,
                    Callback = callback,
                    State = state,
                    Completion = tcs
                });
            }
            _post.Send();
            return tcs.Task;
        }

        public Task PostAsync(Action<ListenerSecondary> callback, ListenerSecondary state)
        {
            var tcs = new TaskCompletionSource<object>();
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _listenerSecondaryCallbackAdapter,
                    Callback = callback,
                    State = state,
                    Completion = tcs
                });
            }
            _post.Send();
            return tcs.Task;
        }

        public void Send(Action<ListenerSecondary> callback, ListenerSecondary state)
        {
            if (_loop.ThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                callback.Invoke(state);
            }
            else
            {
                PostAsync(callback, state).Wait();
            }
        }

        private void PostCloseHandle(Action<IntPtr> callback, IntPtr handle)
        {
            lock (_workSync)
            {
                _closeHandleAdding.Enqueue(new CloseHandle { Callback = callback, Handle = handle });
            }
            _post.Send();
        }

        private void ThreadStart(object parameter)
        {
            var tcs = (TaskCompletionSource<int>)parameter;
            try
            {
                _loop.Init(_engine.Libuv);
                _post.Init(_loop, OnPost);
                tcs.SetResult(0);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return;
            }

            _initCompleted = true;

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

                _engine.Libuv.walk(
                    _loop,
                    (ptr, arg) =>
                    {
                        var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                        if (handle != _post)
                        {
                            handle.Dispose();
                        }
                    },
                    IntPtr.Zero);

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
                        _log.LogError("KestrelThread.DoPostWork", ex);
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
                    _log.LogError("KestrelThread.DoPostCloseHandle", ex);
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
