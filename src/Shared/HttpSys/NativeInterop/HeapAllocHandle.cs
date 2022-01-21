// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal sealed class HeapAllocHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private static readonly IntPtr ProcessHeap = UnsafeNclNativeMethods.GetProcessHeap();

    // Called by P/Invoke when returning SafeHandles
    private HeapAllocHandle()
        : base(ownsHandle: true)
    {
    }

    // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
    protected override bool ReleaseHandle()
    {
        return UnsafeNclNativeMethods.HeapFree(ProcessHeap, 0, handle);
    }
}
