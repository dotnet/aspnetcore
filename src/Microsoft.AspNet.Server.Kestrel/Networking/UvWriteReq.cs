// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    /// <summary>
    /// Summary description for UvWriteRequest
    /// </summary>
    public class UvWriteReq : UvRequest
    {
        private readonly static Libuv.uv_write_cb _uv_write_cb = (IntPtr ptr, int status) => UvWriteCb(ptr, status);

        private IntPtr _bufs;

        private Action<UvWriteReq, int, Exception, object> _callback;
        private object _state;
        private const int BUFFER_COUNT = 4;

        private List<GCHandle> _pins = new List<GCHandle>(BUFFER_COUNT + 1);

        public UvWriteReq(IKestrelTrace logger) : base(logger)
        {
        }

        public void Init(UvLoopHandle loop)
        {
            var requestSize = loop.Libuv.req_size(Libuv.RequestType.WRITE);
            var bufferSize = Marshal.SizeOf<Libuv.uv_buf_t>() * BUFFER_COUNT;
            CreateMemory(
                loop.Libuv,
                loop.ThreadId,
                requestSize + bufferSize);
            _bufs = handle + requestSize;
        }

        public unsafe void Write(
            UvStreamHandle handle,
            MemoryPoolIterator2 start,
            MemoryPoolIterator2 end,
            int nBuffers,
            Action<UvWriteReq, int, Exception, object> callback,
            object state)
        {
            try
            {
                // add GCHandle to keeps this SafeHandle alive while request processing
                _pins.Add(GCHandle.Alloc(this, GCHandleType.Normal));

                var pBuffers = (Libuv.uv_buf_t*)_bufs;
                if (nBuffers > BUFFER_COUNT)
                {
                    // create and pin buffer array when it's larger than the pre-allocated one
                    var bufArray = new Libuv.uv_buf_t[nBuffers];
                    var gcHandle = GCHandle.Alloc(bufArray, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers = (Libuv.uv_buf_t*)gcHandle.AddrOfPinnedObject();
                }

                var block = start.Block;
                for (var index = 0; index < nBuffers; index++)
                {
                    var blockStart = block == start.Block ? start.Index : block.Data.Offset;
                    var blockEnd = block == end.Block ? end.Index : block.BlockEndOffset;

                    // create and pin each segment being written
                    pBuffers[index] = Libuv.buf_init(
                        block.Pin() + blockStart,
                        blockEnd - blockStart);

                    block = block.Next;
                }

                _callback = callback;
                _state = state;
                _uv.write(this, handle, pBuffers, nBuffers, _uv_write_cb);
            }
            catch
            {
                _callback = null;
                _state = null;
                Unpin(this);

                var block = start.Block;
                for (var index = 0; index < nBuffers; index++)
                {
                    block.Unpin();
                    block = block.Next;
                }

                throw;
            }
        }

        public unsafe void Write2(
            UvStreamHandle handle,
            ArraySegment<ArraySegment<byte>> bufs,
            UvStreamHandle sendHandle,
            Action<UvWriteReq, int, Exception, object> callback,
            object state)
        {
            try
            {
                // add GCHandle to keeps this SafeHandle alive while request processing
                _pins.Add(GCHandle.Alloc(this, GCHandleType.Normal));

                var pBuffers = (Libuv.uv_buf_t*)_bufs;
                var nBuffers = bufs.Count;
                if (nBuffers > BUFFER_COUNT)
                {
                    // create and pin buffer array when it's larger than the pre-allocated one
                    var bufArray = new Libuv.uv_buf_t[nBuffers];
                    var gcHandle = GCHandle.Alloc(bufArray, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers = (Libuv.uv_buf_t*)gcHandle.AddrOfPinnedObject();
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
                _uv.write2(this, handle, pBuffers, nBuffers, sendHandle, _uv_write_cb);
            }
            catch
            {
                _callback = null;
                _state = null;
                Unpin(this);
                throw;
            }
        }

        private static void Unpin(UvWriteReq req)
        {
            foreach (var pin in req._pins)
            {
                pin.Free();
            }
            req._pins.Clear();
        }

        private static void UvWriteCb(IntPtr ptr, int status)
        {
            var req = FromIntPtr<UvWriteReq>(ptr);
            Unpin(req);

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
                req._log.LogError("UvWriteCb", ex);
                throw;
            }
        }
    }
}