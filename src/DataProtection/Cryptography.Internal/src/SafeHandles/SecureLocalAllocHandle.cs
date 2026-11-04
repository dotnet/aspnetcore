// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

/// <summary>
/// Represents a handle returned by LocalAlloc.
/// The memory will be zeroed out before it's freed.
/// </summary>
internal sealed unsafe class SecureLocalAllocHandle : LocalAllocHandle
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

#if NETSTANDARD2_0
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
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
