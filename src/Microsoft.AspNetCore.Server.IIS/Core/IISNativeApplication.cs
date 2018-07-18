// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class IISNativeApplication
    {
        private readonly IntPtr _nativeApplication;

        public IISNativeApplication(IntPtr nativeApplication)
        {
            _nativeApplication = nativeApplication;
        }

        public void StopIncomingRequests()
        {
            NativeMethods.HttpStopIncomingRequests(_nativeApplication);
        }

        public void StopCallsIntoManaged()
        {
            NativeMethods.HttpStopCallsIntoManaged(_nativeApplication);
        }

        public void RegisterCallbacks(
            NativeMethods.PFN_REQUEST_HANDLER requestHandler,
            NativeMethods.PFN_SHUTDOWN_HANDLER shutdownHandler,
            NativeMethods.PFN_ASYNC_COMPLETION onAsyncCompletion,
            IntPtr requestContext,
            IntPtr shutdownContext)
        {
            NativeMethods.HttpRegisterCallbacks(
                _nativeApplication,
                requestHandler,
                shutdownHandler,
                onAsyncCompletion, 
                requestContext,
                shutdownContext, 
                out var resetStandardStreams);

            if (resetStandardStreams)
            {
                ResetStdOutHandles();
            }
        }

        private static void ResetStdOutHandles()
        {
            // By using the PipeOutputRedirection, after calling RegisterCallbacks,
            // stdout and stderr will be redirected to NULL. However, if something wrote
            // to stdout before redirecting, (like a Console.WriteLine during startup),
            // we need to call Console.Set* to pick up the modified consoles outputs.
            Console.SetOut(CreateStreamWriter(Console.OpenStandardOutput()));
            Console.SetError(CreateStreamWriter(Console.OpenStandardError()));
        }

        private static StreamWriter CreateStreamWriter(Stream stdStream)
        {
            return new StreamWriter(
                stdStream,
                encoding: Console.OutputEncoding,
                bufferSize: 256,
                leaveOpen: true)
            { AutoFlush = true };
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        ~IISNativeApplication()
        {
            // If this finalize is invoked, try our best to block all calls into managed.
            StopCallsIntoManaged();
        }
    }
}
