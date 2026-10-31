// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed class SafeNativeOverlapped : SafeHandle
{
    internal static readonly SafeNativeOverlapped Zero = new SafeNativeOverlapped();
    private readonly ThreadPoolBoundHandle? _boundHandle;

    private static bool HasShutdownStarted => Environment.HasShutdownStarted
                || AppDomain.CurrentDomain.IsFinalizingForUnload();

    public SafeNativeOverlapped()
        : base(IntPtr.Zero, true)
    {
    }

    internal unsafe SafeNativeOverlapped(ThreadPoolBoundHandle boundHandle, NativeOverlapped* handle)
        : base(IntPtr.Zero, true)
    {
        SetHandle((IntPtr)handle);
        _boundHandle = boundHandle;
    }

    public override bool IsInvalid
    {
        get { return handle == IntPtr.Zero; }
    }

    protected override bool ReleaseHandle()
    {
        Debug.Assert(_boundHandle != null, "ReleaseHandle can't be called on SafeNativeOverlapped.Zero.");

        IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);
        // Do not call free during AppDomain shutdown, there may be an outstanding operation.
        // Overlapped will take care calling free when the native callback completes.
        if (oldHandle != IntPtr.Zero && !HasShutdownStarted)
        {
            unsafe
            {
                _boundHandle.FreeNativeOverlapped((NativeOverlapped*)oldHandle);
            }
        }
        return true;
    }
}
