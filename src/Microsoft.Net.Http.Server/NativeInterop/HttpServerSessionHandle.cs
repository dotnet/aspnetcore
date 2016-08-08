// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="HttpServerSessionHandle.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.Http.Server
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
