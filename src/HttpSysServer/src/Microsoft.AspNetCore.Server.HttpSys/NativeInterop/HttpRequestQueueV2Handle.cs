// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Server.HttpSys
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
            return (HttpApi.HttpCloseRequestQueue(handle) ==
                        UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS);
        }
    }
}
