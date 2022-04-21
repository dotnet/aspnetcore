// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal class NativeSafeHandle : SafeHandle
{
    public override bool IsInvalid => handle == IntPtr.Zero;

    public NativeSafeHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        this.handle = handle;
    }

    protected override bool ReleaseHandle()
    {
        handle = IntPtr.Zero;

        return true;
    }
}
