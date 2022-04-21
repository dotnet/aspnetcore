// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal class IISHttpContextSafeHandle : NativeSafeHandle
{
    public override bool IsInvalid => handle == IntPtr.Zero;

    public IISHttpContextSafeHandle(IntPtr handle) : base(handle)
    {
    }

    internal IISHttpContext? Context { get; set; }

    internal NativeMethods.REQUEST_NOTIFICATION_STATUS Status { get; set; }

    protected override void Dispose(bool disposing)
    {
        NativeMethods.HttpSetManagedRequestComplete(this);

        base.Dispose(disposing);
    }

    protected override bool ReleaseHandle()
    {
        // Dispose the context
        Context!.Dispose();

        // Tell IIS we're done
        // We post completion to the thread pool instead of the IIS thread pool to avoid having
        // new requests compete with existing threads for IIS threads.
        // We're going to use the local queue so that we can resume the state machine on this thread after
        // unwinding.
        ThreadPool.UnsafeQueueUserWorkItem(static state =>
        {
            var (handle, status) = state;

            // This will resume the IIS state machine inline
            NativeMethods.HttpIndicateCompletion(handle, status);
        },
        // We use the native handle because we're going to dispose the safe handle by the time this runs.
        // We don't want to hold onto it since managed code is unwinding.
        (handle, Status), preferLocal: true);

        // Clear the state
        handle = IntPtr.Zero;
        Context = null;

        return true;
    }
}
