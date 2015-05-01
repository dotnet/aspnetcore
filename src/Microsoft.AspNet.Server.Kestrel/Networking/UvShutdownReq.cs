// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    /// <summary>
    /// Summary description for UvShutdownRequest
    /// </summary>
    public class UvShutdownReq : UvReq
    {
        private readonly static Libuv.uv_shutdown_cb _uv_shutdown_cb = UvShutdownCb;

        Action<UvShutdownReq, int, object> _callback;
        object _state;

        public void Init(UvLoopHandle loop)
        {
            CreateMemory(
                loop.Libuv, 
                loop.ThreadId,
                loop.Libuv.req_size(Libuv.RequestType.SHUTDOWN));
        }

        public void Shutdown(UvStreamHandle handle, Action<UvShutdownReq, int, object> callback, object state)
        {
            _callback = callback;
            _state = state;
            _uv.shutdown(this, handle, _uv_shutdown_cb);
        }

        private static void UvShutdownCb(IntPtr ptr, int status)
        {
            var req = FromIntPtr<UvShutdownReq>(ptr);
            req._callback(req, status, req._state);
            req._callback = null;
            req._state = null;
        }
    }
}