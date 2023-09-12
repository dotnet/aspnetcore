// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Microsoft.AspNetCore.Server.HttpSys;

// This class is a wrapper for Http.sys V2 request queue handle.
internal sealed class HttpRequestQueueV2Handle : SafeHandleZeroOrMinusOneIsInvalid
{
    public HttpRequestQueueV2Handle()
        : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
        return PInvoke.HttpCloseRequestQueue((HANDLE)handle) ==
                    UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;
    }
}
