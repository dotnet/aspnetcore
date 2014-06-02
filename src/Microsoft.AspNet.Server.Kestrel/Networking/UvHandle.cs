// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvHandle : SafeHandle
    {
        protected Libuv _uv;
        static Libuv.uv_close_cb _close_cb = DestroyHandle;

        public UvHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        unsafe protected void CreateHandle(Libuv uv, int size)
        {
            _uv = uv;
            handle = Marshal.AllocCoTaskMem(size);
            *(IntPtr*)handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        }

        protected void CreateHandle(UvLoopHandle loop, int size)
        {
            CreateHandle(loop._uv, size);
        }
        protected override bool ReleaseHandle()
        {
            var memory = handle;
            if (memory != IntPtr.Zero)
            {
                _uv.close(this, _close_cb);
                handle = IntPtr.Zero;
            }
            return true;
        }

        unsafe protected static void DestroyHandle(IntPtr memory)
        {
            var gcHandlePtr = *(IntPtr*)memory;
            if (gcHandlePtr != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(gcHandlePtr).Free();
            }
            Marshal.FreeCoTaskMem(memory);
        }
    }
}
