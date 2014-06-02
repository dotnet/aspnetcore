// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvStreamHandle : UvHandle
    {
        private Libuv.uv_connection_cb _connection_cb;
        private Libuv.uv_alloc_cb _alloc_cb;
        private Libuv.uv_read_cb _read_cb;

        private Action<int, UvStreamHandle> _connection;
        private Action<int, UvStreamHandle> _alloc;
        private Action<int, byte[], UvStreamHandle> _read;

        public void Listen(int backlog, Action<int, UvStreamHandle> connection)
        {
            _connection_cb = OnConnection;
            _connection = connection;
            _uv.listen(this, 10, _connection_cb);
        }

        public void OnConnection(IntPtr server, int status)
        {
            _connection(status, this);
        }

        public void Accept(UvStreamHandle handle)
        {
            _uv.accept(this, handle);
        }

        public void ReadStart(Action<int, byte[], UvStreamHandle> read)
        {
            _alloc_cb = OnAlloc;
            _read_cb = OnRead;
            _read = read;
            _uv.read_start(this, _alloc_cb, _read_cb);
        }

        private void OnAlloc(IntPtr server, int suggested_size, out Libuv.uv_buf_t buf)
        {
            buf = new Libuv.uv_buf_t
            {
                memory = Marshal.AllocCoTaskMem(suggested_size),
                len = (uint)suggested_size,
            };
        }

        private void OnRead(IntPtr server, int nread, ref Libuv.uv_buf_t buf)
        {
            if (nread == -4095)
            {
                _read(0, null, this);
                Marshal.FreeCoTaskMem(buf.memory);
                return;
            }
            var length = _uv.Check(nread);
            var data = new byte[length];
            Marshal.Copy(buf.memory, data, 0, length);
            Marshal.FreeCoTaskMem(buf.memory);

            _read(length, data, this);
        }

        public void ReadStop()
        {
            _uv.read_stop(this);
        }
    }
}
