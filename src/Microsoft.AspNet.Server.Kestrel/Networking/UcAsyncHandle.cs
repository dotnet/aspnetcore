// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class UvAsyncHandle : UvHandle
    {
        private static Libuv.uv_async_cb _uv_async_cb = AsyncCb;

        unsafe static void AsyncCb(IntPtr handle)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(*(IntPtr*)handle);
            var self = (UvAsyncHandle)gcHandle.Target;
            self._callback.Invoke();
        }

        private Action _callback;

        public void Init(UvLoopHandle loop, Action callback)
        {
            CreateHandle(loop, 256);
            _callback = callback;
            _uv.async_init(loop, this, _uv_async_cb);
        }

        private void UvAsyncCb(IntPtr handle)
        {
            _callback.Invoke();
        }

        public void Send()
        {
            _uv.async_send(this);
        }
    }
}
