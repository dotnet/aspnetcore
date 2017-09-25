// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Server.HttpSys
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
                    return (HttpApi.HttpCloseServerSession(serverSessionId) ==
                        UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS);
                }
            }
            return true;
        }
    }
}
