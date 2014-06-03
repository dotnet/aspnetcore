// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvStreamHandle : UvHandle
    {
        private readonly static Libuv.uv_connection_cb _uv_connection_cb = UvConnectionCb;
        private readonly static Libuv.uv_alloc_cb _uv_alloc_cb = UvAllocCb;
        private readonly static Libuv.uv_read_cb _uv_read_cb = UvReadCb;

        public Action<UvStreamHandle, int, object> _connectionCallback;
        public object _connectionState;

        public Action<UvStreamHandle, int, byte[], object> _readCallback;
        public object _readState;

        public UvStreamHandle()
        {
        }

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
            Action<UvStreamHandle, int, byte[], object> callback, 
            object state)
        {
            _readCallback = callback;
            _readState = state;
            _uv.read_start(this, _uv_alloc_cb, _uv_read_cb);
        }

        public void ReadStop()
        {
            _uv.read_stop(this);
        }

        private static void UvConnectionCb(IntPtr handle, int status)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);
            stream._connectionCallback(stream, status, stream._connectionState);
        }

        private static void UvAllocCb(IntPtr server, int suggested_size, out Libuv.uv_buf_t buf)
        {
            buf = new Libuv.uv_buf_t
            {
                memory = Marshal.AllocCoTaskMem(suggested_size),
                len = (uint)suggested_size,
            };

        }

        private static void UvReadCb(IntPtr ptr, int nread, ref Libuv.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(ptr);
            if (nread == -4095)
            {
                stream._readCallback(stream, 0, null, stream._readState);
                Marshal.FreeCoTaskMem(buf.memory);
                return;
            }

            var length = stream._uv.Check(nread);
            var data = new byte[length];
            Marshal.Copy(buf.memory, data, 0, length);
            Marshal.FreeCoTaskMem(buf.memory);

            stream._readCallback(stream, length, data, stream._readState);
        }
    }
}
