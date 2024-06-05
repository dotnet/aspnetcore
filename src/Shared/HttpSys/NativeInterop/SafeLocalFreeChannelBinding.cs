// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal sealed class SafeLocalFreeChannelBinding : ChannelBinding
{
    private const int LMEM_FIXED = 0;
    private int size;

    public override int Size
    {
        get { return size; }
    }

    public static SafeLocalFreeChannelBinding LocalAlloc(int cb)
    {
        SafeLocalFreeChannelBinding result;

        result = UnsafeNclNativeMethods.SafeNetHandles.LocalAllocChannelBinding(LMEM_FIXED, (UIntPtr)cb);
        if (result.IsInvalid)
        {
            result.SetHandleAsInvalid();
#pragma warning disable CA2201 // Do not raise reserved exception types
            throw new OutOfMemoryException();
#pragma warning restore CA2201
        }

        result.size = cb;
        return result;
    }

    protected override bool ReleaseHandle()
    {
        return UnsafeNclNativeMethods.SafeNetHandles.LocalFree(handle) == IntPtr.Zero;
    }

    public override bool IsInvalid
    {
        get
        {
            return handle == IntPtr.Zero || handle.ToInt32() == -1;
        }
    }
}
