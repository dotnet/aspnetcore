// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
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
        private static Action<object, object> _objectCallbackAdapter = (callback, state) => ((Action<object>)callback).Invoke(state);
        private KestrelEngine _engine;
        private readonly IApplicationLifetime _appLifetime;
        private Thread _thread;
        private UvLoopHandle _loop;
        private UvAsyncHandle _post;
        private Queue<Work> _workAdding = new Queue<Work>();
        private Queue<Work> _workRunning = new Queue<Work>();
        private Queue<CloseHandle> _closeHandleAdding = new Queue<CloseHandle>();
        private Queue<CloseHandle> _closeHandleRunning = new Queue<CloseHandle>();
        private object _workSync = new Object();
        private bool _stopImmediate = false;
        private bool _initCompleted = false;
        private ExceptionDispatchInfo _closeError;
        private IKestrelTrace _log;

        public KestrelThread(KestrelEngine engine)
        {
            _engine = engine;
            _appLifetime = engine.AppLifetime;
            _log = engine.Log;
            _loop = new UvLoopHandle(_log);
            _post = new UvAsyncHandle(_log);
            _thread = new Thread(ThreadStart);
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

            Post(OnStop, null);
            if (!_thread.Join((int)timeout.TotalMilliseconds))
            {
                try
                {
                    Post(OnStopRude, null);
                    if (!_thread.Join((int)timeout.TotalMilliseconds))
                    {
                        Post(OnStopImmediate, null);
                        if (!_thread.Join((int)timeout.TotalMilliseconds))
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
                    if (!_thread.Join((int)timeout.TotalMilliseconds))
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

        private void OnStop(object obj)
        {
            _post.Unreference();
        }

        private void OnStopRude(object obj)
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

        private void OnStopImmediate(object obj)
        {
            _stopImmediate = true;
            _loop.Stop();
        }

        public void Post(Action<object> callback, object state)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work { CallbackAdapter = _objectCallbackAdapter, Callback = callback, State = state });
            }
            _post.Send();
        }

        public void Post<T>(Action<T> callback, T state)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = (callback2, state2) => ((Action<T>)callback2).Invoke((T)state2),
                    Callback = callback,
                    State = state
                });
            }
            _post.Send();
        }

        public Task PostAsync(Action<object> callback, object state)
        {
            var tcs = new TaskCompletionSource<int>();
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = _objectCallbackAdapter,
                    Callback = callback,
                    State = state,
                    Completion = tcs
                });
            }
            _post.Send();
            return tcs.Task;
        }

        public Task PostAsync<T>(Action<T> callback, T state)
        {
            var tcs = new TaskCompletionSource<int>();
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work
                {
                    CallbackAdapter = (state1, state2) => ((Action<T>)state1).Invoke((T)state2),
                    Callback = callback,
                    State = state,
                    Completion = tcs
                });
            }
            _post.Send();
            return tcs.Task;
        }

        public void Send(Action<object> callback, object state)
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
            DoPostWork();
            DoPostCloseHandle();
        }

        private void DoPostWork()
        {
            Queue<Work> queue;
            lock (_workSync)
            {
                queue = _workAdding;
                _workAdding = _workRunning;
                _workRunning = queue;
            }
            while (queue.Count != 0)
            {
                var work = queue.Dequeue();
                try
                {
                    work.CallbackAdapter(work.Callback, work.State);
                    if (work.Completion != null)
                    {
                        ThreadPool.QueueUserWorkItem(
                            tcs =>
                            {
                                ((TaskCompletionSource<int>)tcs).SetResult(0);
                            },
                            work.Completion);
                    }
                }
                catch (Exception ex)
                {
                    if (work.Completion != null)
                    {
                        ThreadPool.QueueUserWorkItem(_ => work.Completion.SetException(ex), null);
                    }
                    else
                    {
                        _log.LogError("KestrelThread.DoPostWork", ex);
                        throw;
                    }
                }
            }
        }
        private void DoPostCloseHandle()
        {
            Queue<CloseHandle> queue;
            lock (_workSync)
            {
                queue = _closeHandleAdding;
                _closeHandleAdding = _closeHandleRunning;
                _closeHandleRunning = queue;
            }
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
        }

        private struct Work
        {
            public Action<object, object> CallbackAdapter;
            public object Callback;
            public object State;
            public TaskCompletionSource<int> Completion;
        }
        private struct CloseHandle
        {
            public Action<IntPtr> Callback;
            public IntPtr Handle;
        }
    }
}
