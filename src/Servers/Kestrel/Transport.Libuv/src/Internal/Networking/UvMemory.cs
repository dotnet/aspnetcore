// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#define TRACE

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    /// <summary>
    /// Summary description for UvMemory
    /// </summary>
    public abstract class UvMemory : SafeHandle
    {
        protected LibuvFunctions _uv;
        protected int _threadId;
        protected readonly ILibuvTrace _log;
        private readonly GCHandleType _handleType;

        protected UvMemory(ILibuvTrace logger, GCHandleType handleType = GCHandleType.Weak) : base(IntPtr.Zero, true)
        {
            _log = logger;
            _handleType = handleType;
        }

        public LibuvFunctions Libuv { get { return _uv; } }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        public int ThreadId
        {
            get
            {
                return _threadId;
            }
            private set
            {
                _threadId = value;
            }
        }

        unsafe protected void CreateMemory(LibuvFunctions uv, int threadId, int size)
        {
            _uv = uv;
            ThreadId = threadId;

            handle = Marshal.AllocCoTaskMem(size);
            *(IntPtr*)handle = GCHandle.ToIntPtr(GCHandle.Alloc(this, _handleType));
        }

        unsafe protected static void DestroyMemory(IntPtr memory)
        {
            var gcHandlePtr = *(IntPtr*)memory;
            DestroyMemory(memory, gcHandlePtr);
        }

        protected static void DestroyMemory(IntPtr memory, IntPtr gcHandlePtr)
        {
            if (gcHandlePtr != IntPtr.Zero)
            {
                var gcHandle = GCHandle.FromIntPtr(gcHandlePtr);
                gcHandle.Free();
            }
            Marshal.FreeCoTaskMem(memory);
        }

        public IntPtr InternalGetHandle()
        {
            return handle;
        }

        public void Validate(bool closed = false)
        {
            Debug.Assert(closed || !IsClosed, "Handle is closed");
            Debug.Assert(!IsInvalid, "Handle is invalid");
            Debug.Assert(_threadId == Thread.CurrentThread.ManagedThreadId, "ThreadId is incorrect");
        }

        unsafe public static THandle FromIntPtr<THandle>(IntPtr handle)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(*(IntPtr*)handle);
            return (THandle)gcHandle.Target;
        }
    }
}