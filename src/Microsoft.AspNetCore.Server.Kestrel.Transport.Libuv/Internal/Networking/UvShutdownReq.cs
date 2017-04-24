// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    /// <summary>
    /// Summary description for UvShutdownRequest
    /// </summary>
    public class UvShutdownReq : UvRequest
    {
        private readonly static LibuvFunctions.uv_shutdown_cb _uv_shutdown_cb = UvShutdownCb;

        private Action<UvShutdownReq, int, Exception, object> _callback;
        private object _state;
        private LibuvAwaitable<UvShutdownReq> _awaitable = new LibuvAwaitable<UvShutdownReq>();

        public UvShutdownReq(ILibuvTrace logger) : base(logger)
        {
        }

        public override void Init(LibuvThread thread)
        {
            var loop = thread.Loop;

            CreateMemory(
                loop.Libuv,
                loop.ThreadId,
                loop.Libuv.req_size(LibuvFunctions.RequestType.SHUTDOWN));

            base.Init(thread);
        }

        public LibuvAwaitable<UvShutdownReq> ShutdownAsync(UvStreamHandle handle)
        {
            Shutdown(handle, LibuvAwaitable<UvShutdownReq>.Callback, _awaitable);
            return _awaitable;
        }

        public void Shutdown(UvStreamHandle handle, Action<UvShutdownReq, int, Exception, object> callback, object state)
        {
            _callback = callback;
            _state = state;
            _uv.shutdown(this, handle, _uv_shutdown_cb);
        }

        private static void UvShutdownCb(IntPtr ptr, int status)
        {
            var req = FromIntPtr<UvShutdownReq>(ptr);

            var callback = req._callback;
            req._callback = null;

            var state = req._state;
            req._state = null;

            Exception error = null;
            if (status < 0)
            {
                req.Libuv.Check(status, out error);
            }

            try
            {
                callback(req, status, error, state);
            }
            catch (Exception ex)
            {
                req._log.LogError(0, ex, "UvShutdownCb");
                throw;
            }
        }
    }
}