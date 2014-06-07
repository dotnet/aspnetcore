// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvStreamHandle : UvHandle
    {
        private readonly static Libuv.uv_connection_cb _uv_connection_cb = UvConnectionCb;
        private readonly static Libuv.uv_alloc_cb _uv_alloc_cb = UvAllocCb;
        private readonly static Libuv.uv_read_cb _uv_read_cb = UvReadCb;

        public Action<UvStreamHandle, int, object> _connectionCallback;
        public object _connectionState;

        public Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback;

        public Action<UvStreamHandle, int, object> _readCallback;
        public object _readState;


        public void Listen(int backlog, Action<UvStreamHandle, int, object> callback, object state)
        {
            _connectionCallback = callback;
            _connectionState = state;
            _uv.listen(this, 10, _uv_connection_cb);
        }

        public void Accept(UvStreamHandle handle)
        {
            _uv.accept(this, handle);
        }

        public void ReadStart(
            Func<UvStreamHandle, int, object, Libuv.uv_buf_t> allocCallback,
            Action<UvStreamHandle, int, object> readCallback,
            object state)
        {
            _allocCallback = allocCallback;
            _readCallback = readCallback;
            _readState = state;
            _uv.read_start(this, _uv_alloc_cb, _uv_read_cb);
        }

        public void ReadStop()
        {
            _uv.read_stop(this);
        }

        public int TryWrite(Libuv.uv_buf_t buf)
        {
            return _uv.try_write(this, new[] { buf }, 1);
        }


        private static void UvConnectionCb(IntPtr handle, int status)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);
            stream._connectionCallback(stream, status, stream._connectionState);
        }

        private static void UvAllocCb(IntPtr handle, int suggested_size, out Libuv.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);
            buf = stream._allocCallback(stream, suggested_size, stream._readState);
        }

        private static void UvReadCb(IntPtr handle, int nread, ref Libuv.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);

            if (nread == -4095)
            {
                stream._readCallback(stream, 0, stream._readState);
                return;
            }

            var length = stream._uv.Check(nread);

            stream._readCallback(stream, nread, stream._readState);
        }

    }
}
