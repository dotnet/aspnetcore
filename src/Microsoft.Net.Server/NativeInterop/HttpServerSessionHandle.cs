// -----------------------------------------------------------------------
// <copyright file="HttpServerSessionHandle.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.Server
{
    internal sealed class HttpServerSessionHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        private int disposed;
        private ulong serverSessionId;

        internal HttpServerSessionHandle(ulong id)
            : base()
        {
            serverSessionId = id;

            // This class uses no real handle so we need to set a dummy handle. Otherwise, IsInvalid always remains             
            // true.

            SetHandle(new IntPtr(1));
        }

        internal ulong DangerousGetServerSessionId()
        {
            return serverSessionId;
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                if (Interlocked.Increment(ref disposed) == 1)
                {
                    // Closing server session also closes all open url groups under that server session.
                    return (UnsafeNclNativeMethods.HttpApi.HttpCloseServerSession(serverSessionId) ==
                        UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS);
                }
            }
            return true;
        }
    }
}
