// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles
{
    /// <summary>
    /// Represents a handle returned by LocalAlloc.
    /// The memory will be zeroed out before it's freed.
    /// </summary>
    internal unsafe sealed class SecureLocalAllocHandle : LocalAllocHandle
    {
        private readonly IntPtr _cb;

        private SecureLocalAllocHandle(IntPtr cb)
        {
            _cb = cb;
        }

        public IntPtr Length
        {
            get
            {
                return _cb;
            }
        }

        /// <summary>
        /// Allocates some amount of memory using LocalAlloc.
        /// </summary>
        public static SecureLocalAllocHandle Allocate(IntPtr cb)
        {
            SecureLocalAllocHandle newHandle = new SecureLocalAllocHandle(cb);
            newHandle.AllocateImpl(cb);
            return newHandle;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private void AllocateImpl(IntPtr cb)
        {
            handle = Marshal.AllocHGlobal(cb); // actually calls LocalAlloc
        }

        public SecureLocalAllocHandle Duplicate()
        {
            SecureLocalAllocHandle duplicateHandle = Allocate(_cb);
            UnsafeBufferUtil.BlockCopy(from: this, to: duplicateHandle, length: _cb);
            return duplicateHandle;
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            UnsafeBufferUtil.SecureZeroMemory((byte*)handle, _cb); // compiler won't optimize this away
            return base.ReleaseHandle();
        }
    }
}
