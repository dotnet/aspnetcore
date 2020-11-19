// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

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

        public void RegisterCallbacks(
            NativeMethods.PFN_REQUEST_HANDLER requestHandler,
            NativeMethods.PFN_SHUTDOWN_HANDLER shutdownHandler,
            NativeMethods.PFN_DISCONNECT_HANDLER disconnectHandler,
            NativeMethods.PFN_ASYNC_COMPLETION onAsyncCompletion,
            NativeMethods.PFN_REQUESTS_DRAINED_HANDLER requestDrainedHandler,
            IntPtr requestContext,
            IntPtr shutdownContext)
        {
            NativeMethods.HttpRegisterCallbacks(
                _nativeApplication,
                requestHandler,
                shutdownHandler,
                disconnectHandler,
                onAsyncCompletion,
                requestDrainedHandler,
                requestContext,
                shutdownContext);
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
