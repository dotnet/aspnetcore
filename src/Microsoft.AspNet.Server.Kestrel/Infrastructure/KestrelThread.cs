// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for KestrelThread
    /// </summary>
    public class KestrelThread
    {
        private static Action<object, object> _objectCallbackAdapter = (callback, state) => ((Action<object>)callback).Invoke(state);
        private KestrelEngine _engine;
        private readonly IApplicationShutdown _appShutdown;
        private Thread _thread;
        private UvLoopHandle _loop;
        private UvAsyncHandle _post;
        private Queue<Work> _workAdding = new Queue<Work>();
        private Queue<Work> _workRunning = new Queue<Work>();
        private Queue<CloseHandle> _closeHandleAdding = new Queue<CloseHandle>();
        private Queue<CloseHandle> _closeHandleRunning = new Queue<CloseHandle>();
        private object _workSync = new Object();
        private bool _stopImmediate = false;
        private ExceptionDispatchInfo _closeError;

        public KestrelThread(KestrelEngine engine, ServiceContext serviceContext)
        {
            _engine = engine;
            _appShutdown = serviceContext.AppShutdown;
            _loop = new UvLoopHandle();
            _post = new UvAsyncHandle();
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
            Post(OnStop, null);
            if (!_thread.Join((int)timeout.TotalMilliseconds))
            {
                Post(OnStopRude, null);
                if (!_thread.Join((int)timeout.TotalMilliseconds))
                {
                    Post(OnStopImmediate, null);
                    if (!_thread.Join((int)timeout.TotalMilliseconds))
                    {
#if DNX451
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
                _post.DangerousClose();

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

                // Ensure the "DangerousClose" operation completes in the event loop.
                var ran2 = _loop.Run();

                _loop.Dispose();
            }
            catch (Exception ex)
            {
                _closeError = ExceptionDispatchInfo.Capture(ex);
                // Request shutdown so we can rethrow this exception
                // in Stop which should be observable.
                _appShutdown.RequestShutdown();
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
                        Trace.WriteLine("KestrelThread.DoPostWork " + ex.ToString());
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
                    Trace.WriteLine("KestrelThread.DoPostCloseHandle " + ex.ToString());
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
