// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class UvAsyncHandle : UvHandle
    {
        private static Libuv.uv_async_cb _uv_async_cb = (handle) => AsyncCb(handle);
        private Action _callback;

        public UvAsyncHandle(IKestrelTrace logger) : base(logger)
        {
        }

        public void Init(UvLoopHandle loop, Action callback)
        {
            CreateMemory(
                loop.Libuv, 
                loop.ThreadId, 
                loop.Libuv.handle_size(Libuv.HandleType.ASYNC));

            _callback = callback;
            _uv.async_init(loop, this, _uv_async_cb);
        }

        public void Send()
        {
            _uv.async_send(this);
        }

        unsafe private static void AsyncCb(IntPtr handle)
        {
            FromIntPtr<UvAsyncHandle>(handle)._callback.Invoke();
        }
    }
}
