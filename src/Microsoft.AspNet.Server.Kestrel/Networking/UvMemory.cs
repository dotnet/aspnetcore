// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    /// <summary>
    /// Summary description for UvMemory
    /// </summary>
    public abstract class UvMemory : SafeHandle
    {
        protected Libuv _uv;
        public UvMemory() : base(IntPtr.Zero, true)
        {
        }

        public Libuv Libuv { get { return _uv; } }

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

        unsafe protected static void DestroyHandle(IntPtr memory)
        {
            var gcHandlePtr = *(IntPtr*)memory;
            if (gcHandlePtr != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(gcHandlePtr).Free();
            }
            Marshal.FreeCoTaskMem(memory);
        }

        unsafe public static THandle FromIntPtr<THandle>(IntPtr handle)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(*(IntPtr*)handle);
            return (THandle)gcHandle.Target;
        }

    }
}