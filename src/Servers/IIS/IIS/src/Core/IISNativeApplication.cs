// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class IISNativeApplication
{
    private readonly NativeSafeHandle _nativeApplication;
    private bool _hasRegisteredCallbacks;
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

    public void Stop()
    {
        lock (_sync)
        {
            if (_nativeApplication.IsInvalid)
            {
                return;
            }

            if (_hasRegisteredCallbacks)
            {
                NativeMethods.HttpStopCallsIntoManaged(_nativeApplication);
            }

            _nativeApplication.Dispose();
            GC.SuppressFinalize(this);
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
        _hasRegisteredCallbacks = true;

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

    ~IISNativeApplication()
    {
        // If this finalize is invoked, try our best to block all calls into managed.
        Stop();
    }
}
