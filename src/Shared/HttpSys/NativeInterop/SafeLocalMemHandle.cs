// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal SafeLocalMemHandle()
        : base(true)
    {
    }

    internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle)
        : base(ownsHandle)
    {
        SetHandle(existingHandle);
    }

    protected override bool ReleaseHandle()
    {
        return UnsafeNclNativeMethods.SafeNetHandles.LocalFree(handle) == IntPtr.Zero;
    }
}
