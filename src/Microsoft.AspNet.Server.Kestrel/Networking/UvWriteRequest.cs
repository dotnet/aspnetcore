// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    /// <summary>
    /// Summary description for UvWriteRequest
    /// </summary>
    public class UvWriteReq : UvReq
    {
        private readonly static Libuv.uv_write_cb _uv_write_cb = UvWriteCb;

        Action<UvWriteReq, int, object> _callback;
        object _state;
            
        public void Init(UvLoopHandle loop)
        {
            CreateHandle(loop, loop.Libuv.req_size(2));
        }

        public void Write(UvStreamHandle handle, Libuv.uv_buf_t[] bufs, int nbufs, Action<UvWriteReq, int, object> callback, object state)
        {
            _callback = callback;
            _state = state;
            _uv.write(this, handle, bufs, nbufs, _uv_write_cb);
        }

        private static void UvWriteCb(IntPtr ptr, int status)
        {
            var req = FromIntPtr<UvWriteReq>(ptr);
            req._callback(req, status, req._state);
            req._callback = null;
            req._state = null;
        }
    }

    public abstract class UvReq : UvMemory
    {
        protected override bool ReleaseHandle()
        {
            DestroyHandle(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }
}