// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class IISNativeApplication
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
            delegate* unmanaged<nint, nint, NativeMethods.REQUEST_NOTIFICATION_STATUS> requestCallback,
            delegate* unmanaged<nint, int> shutdownCallback,
            delegate* unmanaged<nint, void> disconnectCallback,
            delegate* unmanaged<nint, int, int, NativeMethods.REQUEST_NOTIFICATION_STATUS> asyncCallback,
            delegate* unmanaged<nint, void> requestsDrainedHandler,
            nint pvRequestContext,
            nint pvShutdownContext)
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
}
