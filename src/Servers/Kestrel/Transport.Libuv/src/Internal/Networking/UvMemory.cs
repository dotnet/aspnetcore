// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    internal abstract class UvMemory : SafeHandle
    {
        protected LibuvFunctions _uv;
        protected int _threadId;
        protected readonly LibuvTrace _log;
        private readonly GCHandleType _handleType;

        protected UvMemory(LibuvTrace logger, GCHandleType handleType = GCHandleType.Weak) : base(IntPtr.Zero, true)
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

        protected unsafe void CreateMemory(LibuvFunctions uv, int threadId, int size)
        {
            _uv = uv;
            ThreadId = threadId;

            handle = Marshal.AllocCoTaskMem(size);
            *(IntPtr*)handle = GCHandle.ToIntPtr(GCHandle.Alloc(this, _handleType));
        }

        protected static unsafe void DestroyMemory(IntPtr memory)
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
            Debug.Assert(_threadId == Environment.CurrentManagedThreadId, "ThreadId is incorrect");
        }

        public static unsafe THandle FromIntPtr<THandle>(IntPtr handle)
        {
            GCHandle gcHandle = GCHandle.FromIntPtr(*(IntPtr*)handle);
            return (THandle)gcHandle.Target;
        }
    }
}
