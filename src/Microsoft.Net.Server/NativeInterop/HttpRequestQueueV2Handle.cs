// -----------------------------------------------------------------------
// <copyright file="HttpRequestQueueV2Handle.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.Server
{
    // This class is a wrapper for Http.sys V2 request queue handle.
    internal sealed class HttpRequestQueueV2Handle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private HttpRequestQueueV2Handle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return (UnsafeNclNativeMethods.SafeNetHandles.HttpCloseRequestQueue(handle) ==
                        UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS);
        }
    }
}
