// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers
{
    internal class MockLibuv : LibuvFunctions
    {
        private UvAsyncHandle _postHandle;
        private uv_async_cb _onPost;

        private readonly object _postLock = new object();
        private TaskCompletionSource<object> _onPostTcs = new TaskCompletionSource<object>();
        private bool _completedOnPostTcs;

        private bool _stopLoop;
        private readonly ManualResetEventSlim _loopWh = new ManualResetEventSlim();

        private readonly string _stackTrace;

        unsafe public MockLibuv()
            : base(onlyForTesting: true)
        {
            _stackTrace = Environment.StackTrace;

            OnWrite = (socket, buffers, triggerCompleted) =>
            {
                triggerCompleted(0);
                return 0;
            };

            _uv_write = UvWrite;

            _uv_async_send = postHandle =>
            {
                lock (_postLock)
                {
                    if (_completedOnPostTcs)
                    {
                        _onPostTcs = new TaskCompletionSource<object>();
                        _completedOnPostTcs = false;
                    }

                    PostCount++;

                    _loopWh.Set();
                }

                return 0;
            };

            _uv_async_init = (loop, postHandle, callback) =>
            {
                _postHandle = postHandle;
                _onPost = callback;

                return 0;
            };

            _uv_run = (loopHandle, mode) =>
            {
                while (!_stopLoop)
                {
                    _loopWh.Wait();
                    KestrelThreadBlocker.Wait();

                    lock (_postLock)
                    {
                        _loopWh.Reset();
                    }

                    _onPost(_postHandle.InternalGetHandle());

                    lock (_postLock)
                    {
                        // Allow the loop to be run again before completing
                        // _onPostTcs given a nested uv_async_send call.
                        if (!_loopWh.IsSet)
                        {
                            // Ensure any subsequent calls to uv_async_send
                            // create a new _onPostTcs to be completed.
                            _completedOnPostTcs = true;

                            // Calling TrySetResult outside the lock to avoid deadlock
                            // when the code attempts to call uv_async_send after awaiting
                            // OnPostTask. Task.Run so the run loop doesn't block either.
                            var onPostTcs = _onPostTcs;
                            Task.Run(() => onPostTcs.TrySetResult(null));
                        }
                    }
                }

                return 0;
            };

            _uv_ref = handle => { };
            _uv_unref = handle =>
            {
                _stopLoop = true;
                _loopWh.Set();
            };

            _uv_stop = handle =>
            {
                _stopLoop = true;
                _loopWh.Set();
            };

            _uv_req_size = reqType => IntPtr.Size;
            _uv_loop_size = () => IntPtr.Size;
            _uv_handle_size = handleType => IntPtr.Size;
            _uv_loop_init = loop => 0;
            _uv_tcp_init = (loopHandle, tcpHandle) => 0;
            _uv_close = (handle, callback) => callback(handle);
            _uv_loop_close = handle => 0;
            _uv_walk = (loop, callback, ignore) => 0;
            _uv_err_name = errno => IntPtr.Zero;
            _uv_strerror = errno => IntPtr.Zero;
            _uv_read_start = UvReadStart;
            _uv_read_stop = (handle) =>
            {
                AllocCallback = null;
                ReadCallback = null;
                return 0;
            };
            _uv_unsafe_async_send = handle =>
            {
                throw new Exception($"Why is this getting called?{Environment.NewLine}{_stackTrace}");
            };

            _uv_timer_init = (loop, handle) => 0;
            _uv_timer_start = (handle, callback, timeout, repeat) => 0;
            _uv_timer_stop = handle => 0;
            _uv_now = (loop) => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public Func<UvStreamHandle, int, Action<int>, int> OnWrite { get; set; }

        public uv_alloc_cb AllocCallback { get; set; }

        public uv_read_cb ReadCallback { get; set; }

        public int PostCount { get; set; }

        public Task OnPostTask => _onPostTcs.Task;

        public ManualResetEventSlim KestrelThreadBlocker { get; } = new ManualResetEventSlim(true);

        private int UvReadStart(UvStreamHandle handle, uv_alloc_cb allocCallback, uv_read_cb readCallback)
        {
            AllocCallback = allocCallback;
            ReadCallback = readCallback;
            return 0;
        }

        unsafe private int UvWrite(UvRequest req, UvStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb)
        {
            return OnWrite(handle, nbufs, status => cb(req.InternalGetHandle(), status));
        }
    }
}
