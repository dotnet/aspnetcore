// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    /// <summary>
    /// Summary description for UvMemory
    /// </summary>
    public abstract class UvMemory : SafeHandle
    {
        protected Libuv _uv;
        int _threadId;

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
            _threadId = Thread.CurrentThread.ManagedThreadId;

            handle = Marshal.AllocCoTaskMem(size);
            *(IntPtr*)handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        }

        protected void CreateHandle(UvLoopHandle loop, int size)
        {
            CreateHandle(loop._uv, size);
            _threadId = loop._threadId;
        }


        public void Validate(bool closed = false)
        {
            Trace.Assert(closed || !IsClosed, "Handle is closed");
            Trace.Assert(!IsInvalid, "Handle is invalid");
            Trace.Assert(_threadId == Thread.CurrentThread.ManagedThreadId, "ThreadId is correct");
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