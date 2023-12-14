// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class IISNativeApplication
{
    private readonly NativeSafeHandle _nativeApplication;
    private readonly object _sync = new object();

    public IISNativeApplication(NativeSafeHandle nativeApplication)
    {
        _nativeApplication = nativeApplication;
    }

    public void StopIncomingRequests()
    {
        lock (_sync)
        {
            if (!_nativeApplication.IsInvalid)
            {
                NativeMethods.HttpStopIncomingRequests(_nativeApplication);
            }
        }
    }

    public void StopCallsIntoManaged()
    {
        lock (_sync)
        {
            if (!_nativeApplication.IsInvalid)
            {
                NativeMethods.HttpStopCallsIntoManaged(_nativeApplication);
            }
        }
    }

    public unsafe void RegisterCallbacks(
        delegate* unmanaged<IntPtr, IntPtr, NativeMethods.REQUEST_NOTIFICATION_STATUS> requestCallback,
        delegate* unmanaged<IntPtr, int> shutdownCallback,
        delegate* unmanaged<IntPtr, void> disconnectCallback,
        delegate* unmanaged<IntPtr, int, int, NativeMethods.REQUEST_NOTIFICATION_STATUS> asyncCallback,
        delegate* unmanaged<IntPtr, void> requestsDrainedHandler,
        IntPtr pvRequestContext,
        IntPtr pvShutdownContext)
    {
        NativeMethods.HttpRegisterCallbacks(
            _nativeApplication,
            requestCallback,
            shutdownCallback,
            disconnectCallback,
            asyncCallback,
            requestsDrainedHandler,
            pvRequestContext,
            pvShutdownContext);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            GC.SuppressFinalize(this);

            // Don't need to await here because pinvokes should never been called after disposing the safe handle.
            _nativeApplication.Dispose();
        }
    }

    ~IISNativeApplication()
    {
        // If this finalize is invoked, try our best to block all calls into managed.
        StopCallsIntoManaged();
    }
}
