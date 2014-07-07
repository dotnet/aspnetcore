// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvStreamHandle : UvHandle
    {
        private readonly static Libuv.uv_connection_cb _uv_connection_cb = UvConnectionCb;
        private readonly static Libuv.uv_alloc_cb _uv_alloc_cb = UvAllocCb;
        private readonly static Libuv.uv_read_cb _uv_read_cb = UvReadCb;

        public Action<UvStreamHandle, int, Exception, object> _connectionCallback;
        public object _connectionState;

        public Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback;

        public Action<UvStreamHandle, int, Exception, object> _readCallback;
        public object _readState;


        public void Listen(int backlog, Action<UvStreamHandle, int, Exception, object> callback, object state)
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
            Action<UvStreamHandle, int, Exception, object> readCallback,
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

            Exception error;
            status = stream.Libuv.Check(status, out error);

            try
            {
                stream._connectionCallback(stream, status, error, stream._connectionState);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UvConnectionCb " + ex.ToString());
            }
        }


        private static void UvAllocCb(IntPtr handle, int suggested_size, out Libuv.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);
            try
            {
                buf = stream._allocCallback(stream, suggested_size, stream._readState);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UvAllocCb " + ex.ToString());
                buf = stream.Libuv.buf_init(IntPtr.Zero, 0);
                throw;
            }
        }

        private static void UvReadCb(IntPtr handle, int nread, ref Libuv.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);

            try
            {
                if (nread < 0)
                {
                    Exception error;
                    stream._uv.Check(nread, out error);
                    stream._readCallback(stream, 0, error, stream._readState);
                }
                else
                {
                    stream._readCallback(stream, nread, null, stream._readState);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("UbReadCb " + ex.ToString());
            }
        }

    }
}
