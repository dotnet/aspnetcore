// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal abstract class UvStreamHandle : UvHandle
    {
        private readonly static LibuvFunctions.uv_connection_cb _uv_connection_cb = (handle, status) => UvConnectionCb(handle, status);
        // Ref and out lamda params must be explicitly typed
        private readonly static LibuvFunctions.uv_alloc_cb _uv_alloc_cb = (IntPtr handle, int suggested_size, out LibuvFunctions.uv_buf_t buf) => UvAllocCb(handle, suggested_size, out buf);
        private readonly static LibuvFunctions.uv_read_cb _uv_read_cb = (IntPtr handle, int status, ref LibuvFunctions.uv_buf_t buf) => UvReadCb(handle, status, ref buf);

        private Action<UvStreamHandle, int, UvException, object> _listenCallback;
        private object _listenState;
        private GCHandle _listenVitality;

        private Func<UvStreamHandle, int, object, LibuvFunctions.uv_buf_t> _allocCallback;
        private Action<UvStreamHandle, int, object> _readCallback;
        private object _readState;
        private GCHandle _readVitality;

        protected UvStreamHandle(ILibuvTrace logger) : base(logger)
        {
        }

        protected override bool ReleaseHandle()
        {
            if (_listenVitality.IsAllocated)
            {
                _listenVitality.Free();
            }
            if (_readVitality.IsAllocated)
            {
                _readVitality.Free();
            }
            return base.ReleaseHandle();
        }

        public void Listen(int backlog, Action<UvStreamHandle, int, UvException, object> callback, object state)
        {
            if (_listenVitality.IsAllocated)
            {
                throw new InvalidOperationException("TODO: Listen may not be called more than once");
            }
            try
            {
                _listenCallback = callback;
                _listenState = state;
                _listenVitality = GCHandle.Alloc(this, GCHandleType.Normal);
                _uv.listen(this, backlog, _uv_connection_cb);
            }
            catch
            {
                _listenCallback = null;
                _listenState = null;
                if (_listenVitality.IsAllocated)
                {
                    _listenVitality.Free();
                }
                throw;
            }
        }

        public void Accept(UvStreamHandle handle)
        {
            _uv.accept(this, handle);
        }

        public void ReadStart(
            Func<UvStreamHandle, int, object, LibuvFunctions.uv_buf_t> allocCallback,
            Action<UvStreamHandle, int, object> readCallback,
            object state)
        {
            if (_readVitality.IsAllocated)
            {
                throw new InvalidOperationException("TODO: ReadStop must be called before ReadStart may be called again");
            }

            try
            {
                _allocCallback = allocCallback;
                _readCallback = readCallback;
                _readState = state;
                _readVitality = GCHandle.Alloc(this, GCHandleType.Normal);
                _uv.read_start(this, _uv_alloc_cb, _uv_read_cb);
            }
            catch
            {
                _allocCallback = null;
                _readCallback = null;
                _readState = null;
                if (_readVitality.IsAllocated)
                {
                    _readVitality.Free();
                }
                throw;
            }
        }

        // UvStreamHandle.ReadStop() should be idempotent to match uv_read_stop()
        public void ReadStop()
        {
            if (_readVitality.IsAllocated)
            {
                _readVitality.Free();
            }
            _allocCallback = null;
            _readCallback = null;
            _readState = null;
            _uv.read_stop(this);
        }

        public int TryWrite(LibuvFunctions.uv_buf_t buf)
        {
            return _uv.try_write(this, new[] { buf }, 1);
        }

        private static void UvConnectionCb(IntPtr handle, int status)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);

            stream.Libuv.Check(status, out var error);

            try
            {
                stream._listenCallback(stream, status, error, stream._listenState);
            }
            catch (Exception ex)
            {
                stream._log.LogError(0, ex, "UvConnectionCb");
                throw;
            }
        }

        private static void UvAllocCb(IntPtr handle, int suggested_size, out LibuvFunctions.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);
            try
            {
                buf = stream._allocCallback(stream, suggested_size, stream._readState);
            }
            catch (Exception ex)
            {
                stream._log.LogError(0, ex, "UvAllocCb");
                buf = stream.Libuv.buf_init(IntPtr.Zero, 0);
                throw;
            }
        }

        private static void UvReadCb(IntPtr handle, int status, ref LibuvFunctions.uv_buf_t buf)
        {
            var stream = FromIntPtr<UvStreamHandle>(handle);

            try
            {
                stream._readCallback(stream, status, stream._readState);
            }
            catch (Exception ex)
            {
                stream._log.LogError(0, ex, "UbReadCb");
                throw;
            }
        }

    }
}
