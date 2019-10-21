// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    /// <summary>
    /// Summary description for UvWriteRequest
    /// </summary>
    internal class UvWriteReq : UvRequest
    {
        private static readonly LibuvFunctions.uv_write_cb _uv_write_cb = (IntPtr ptr, int status) => UvWriteCb(ptr, status);

        private IntPtr _bufs;

        private Action<UvWriteReq, int, UvException, object> _callback;
        private object _state;
        private const int BUFFER_COUNT = 4;

        private LibuvAwaitable<UvWriteReq> _awaitable = new LibuvAwaitable<UvWriteReq>();
        private List<GCHandle> _pins = new List<GCHandle>(BUFFER_COUNT + 1);
        private List<MemoryHandle> _handles = new List<MemoryHandle>(BUFFER_COUNT + 1);

        public UvWriteReq(ILibuvTrace logger) : base(logger)
        {
        }

        public override void Init(LibuvThread thread)
        {
            DangerousInit(thread.Loop);

            base.Init(thread);
        }

        public void DangerousInit(UvLoopHandle loop)
        {
            var requestSize = loop.Libuv.req_size(LibuvFunctions.RequestType.WRITE);
            var bufferSize = Marshal.SizeOf<LibuvFunctions.uv_buf_t>() * BUFFER_COUNT;
            CreateMemory(
                loop.Libuv,
                loop.ThreadId,
                requestSize + bufferSize);
            _bufs = handle + requestSize;
        }

        public LibuvAwaitable<UvWriteReq> WriteAsync(UvStreamHandle handle, in ReadOnlySequence<byte> buffer)
        {
            Write(handle, buffer, LibuvAwaitable<UvWriteReq>.Callback, _awaitable);
            return _awaitable;
        }

        public LibuvAwaitable<UvWriteReq> WriteAsync(UvStreamHandle handle, ArraySegment<ArraySegment<byte>> bufs)
        {
            Write(handle, bufs, LibuvAwaitable<UvWriteReq>.Callback, _awaitable);
            return _awaitable;
        }

        private unsafe void Write(
            UvStreamHandle handle,
            in ReadOnlySequence<byte> buffer,
            Action<UvWriteReq, int, UvException, object> callback,
            object state)
        {
            try
            {
                var nBuffers = 0;
                if (buffer.IsSingleSegment)
                {
                    nBuffers = 1;
                }
                else
                {
                    foreach (var _ in buffer)
                    {
                        nBuffers++;
                    }
                }

                var pBuffers = (LibuvFunctions.uv_buf_t*)_bufs;
                if (nBuffers > BUFFER_COUNT)
                {
                    // create and pin buffer array when it's larger than the pre-allocated one
                    var bufArray = new LibuvFunctions.uv_buf_t[nBuffers];
                    var gcHandle = GCHandle.Alloc(bufArray, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers = (LibuvFunctions.uv_buf_t*)gcHandle.AddrOfPinnedObject();
                }

                if (nBuffers == 1)
                {
                    var memory = buffer.First;
                    var memoryHandle = memory.Pin();
                    _handles.Add(memoryHandle);

                    // Fast path for single buffer
                    pBuffers[0] = Libuv.buf_init(
                            (IntPtr)memoryHandle.Pointer,
                            memory.Length);
                }
                else
                {
                    var index = 0;
                    foreach (var memory in buffer)
                    {
                        // This won't actually pin the buffer since we're already using pinned memory
                        var memoryHandle = memory.Pin();
                        _handles.Add(memoryHandle);

                        // create and pin each segment being written
                        pBuffers[index] = Libuv.buf_init(
                            (IntPtr)memoryHandle.Pointer,
                            memory.Length);
                        index++;
                    }
                }

                _callback = callback;
                _state = state;
                _uv.write(this, handle, pBuffers, nBuffers, _uv_write_cb);
            }
            catch
            {
                _callback = null;
                _state = null;
                UnpinGcHandles();
                throw;
            }
        }

        private void Write(
            UvStreamHandle handle,
            ArraySegment<ArraySegment<byte>> bufs,
            Action<UvWriteReq, int, UvException, object> callback,
            object state)
        {
            WriteArraySegmentInternal(handle, bufs, sendHandle: null, callback: callback, state: state);
        }

        public void Write2(
            UvStreamHandle handle,
            ArraySegment<ArraySegment<byte>> bufs,
            UvStreamHandle sendHandle,
            Action<UvWriteReq, int, UvException, object> callback,
            object state)
        {
            WriteArraySegmentInternal(handle, bufs, sendHandle, callback, state);
        }

        private unsafe void WriteArraySegmentInternal(
            UvStreamHandle handle,
            ArraySegment<ArraySegment<byte>> bufs,
            UvStreamHandle sendHandle,
            Action<UvWriteReq, int, UvException, object> callback,
            object state)
        {
            try
            {
                var pBuffers = (LibuvFunctions.uv_buf_t*)_bufs;
                var nBuffers = bufs.Count;
                if (nBuffers > BUFFER_COUNT)
                {
                    // create and pin buffer array when it's larger than the pre-allocated one
                    var bufArray = new LibuvFunctions.uv_buf_t[nBuffers];
                    var gcHandle = GCHandle.Alloc(bufArray, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers = (LibuvFunctions.uv_buf_t*)gcHandle.AddrOfPinnedObject();
                }

                for (var index = 0; index < nBuffers; index++)
                {
                    // create and pin each segment being written
                    var buf = bufs.Array[bufs.Offset + index];

                    var gcHandle = GCHandle.Alloc(buf.Array, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers[index] = Libuv.buf_init(
                        gcHandle.AddrOfPinnedObject() + buf.Offset,
                        buf.Count);
                }

                _callback = callback;
                _state = state;

                if (sendHandle == null)
                {
                    _uv.write(this, handle, pBuffers, nBuffers, _uv_write_cb);
                }
                else
                {
                    _uv.write2(this, handle, pBuffers, nBuffers, sendHandle, _uv_write_cb);
                }
            }
            catch
            {
                _callback = null;
                _state = null;
                UnpinGcHandles();
                throw;
            }
        }

        // Safe handle has instance method called Unpin
        // so using UnpinGcHandles to avoid conflict
        private void UnpinGcHandles()
        {
            var pinList = _pins;
            var count = pinList.Count;
            for (var i = 0; i < count; i++)
            {
                pinList[i].Free();
            }
            pinList.Clear();

            var handleList = _handles;
            count = handleList.Count;
            for (var i = 0; i < count; i++)
            {
                handleList[i].Dispose();
            }
            handleList.Clear();
        }

        private static void UvWriteCb(IntPtr ptr, int status)
        {
            var req = FromIntPtr<UvWriteReq>(ptr);
            req.UnpinGcHandles();

            var callback = req._callback;
            req._callback = null;

            var state = req._state;
            req._state = null;

            UvException error = null;
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
                req._log.LogError(0, ex, "UvWriteCb");
                throw;
            }
        }
    }
}
