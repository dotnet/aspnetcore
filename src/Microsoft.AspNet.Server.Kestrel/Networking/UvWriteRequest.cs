// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    /// <summary>
    /// Summary description for UvWriteRequest
    /// </summary>
    public class UvWriteReq : UvReq
    {
        private readonly static Libuv.uv_write_cb _uv_write_cb = UvWriteCb;

        IntPtr _bufs;

        Action<UvWriteReq, int, Exception, object> _callback;
        object _state;
        const int BUFFER_COUNT = 4;

        List<GCHandle> _pins = new List<GCHandle>();

        public void Init(UvLoopHandle loop)
        {
            var requestSize = loop.Libuv.req_size(Libuv.RequestType.WRITE);
            var bufferSize = Marshal.SizeOf(typeof(Libuv.uv_buf_t)) * BUFFER_COUNT;
            CreateHandle(loop, requestSize + bufferSize);
            _bufs = handle + requestSize;
        }

        public unsafe void Write(
            UvStreamHandle handle,
            ArraySegment<ArraySegment<byte>> bufs,
            Action<UvWriteReq, int, Exception, object> callback,
            object state)
        {
            var pBuffers = (Libuv.uv_buf_t*)_bufs;
            var nBuffers = bufs.Count;
            if (nBuffers > BUFFER_COUNT)
            {
                var bufArray = new Libuv.uv_buf_t[nBuffers];
                var gcHandle = GCHandle.Alloc(bufArray, GCHandleType.Pinned);
                _pins.Add(gcHandle);
                pBuffers = (Libuv.uv_buf_t*)gcHandle.AddrOfPinnedObject();
            }
            for (var index = 0; index != nBuffers; ++index)
            {
                var buf = bufs.Array[bufs.Offset + index];

                var gcHandle = GCHandle.Alloc(buf.Array, GCHandleType.Pinned);
                _pins.Add(gcHandle);
                pBuffers[index] = Libuv.buf_init(
                    gcHandle.AddrOfPinnedObject() + buf.Offset,
                    buf.Count);
            }
            _callback = callback;
            _state = state;
            _uv.write(this, handle, pBuffers, nBuffers, _uv_write_cb);
        }

        private static void UvWriteCb(IntPtr ptr, int status)
        {
            var req = FromIntPtr<UvWriteReq>(ptr);
            foreach (var pin in req._pins)
            {
                pin.Free();
            }
            req._pins.Clear();

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
                Trace.WriteLine("UvWriteCb " + ex.ToString());
            }
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