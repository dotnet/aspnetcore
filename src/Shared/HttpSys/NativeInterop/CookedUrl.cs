// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.HttpSys.Internal;

// Note this type should only be used while the request buffer remains pinned
internal readonly struct CookedUrl
{
    private readonly HttpApiTypes.HTTP_COOKED_URL _nativeCookedUrl;

    internal CookedUrl(HttpApiTypes.HTTP_COOKED_URL nativeCookedUrl)
    {
        _nativeCookedUrl = nativeCookedUrl;
    }

    internal unsafe string? GetFullUrl()
    {
        if (_nativeCookedUrl.pFullUrl != null && _nativeCookedUrl.FullUrlLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pFullUrl, _nativeCookedUrl.FullUrlLength / 2);
        }
        return null;
    }

    internal unsafe string? GetHost()
    {
        if (_nativeCookedUrl.pHost != null && _nativeCookedUrl.HostLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pHost, _nativeCookedUrl.HostLength / 2);
        }
        return null;
    }

    internal unsafe string? GetAbsPath()
    {
        if (_nativeCookedUrl.pAbsPath != null && _nativeCookedUrl.AbsPathLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pAbsPath, _nativeCookedUrl.AbsPathLength / 2);
        }
        return null;
    }

    internal unsafe string? GetQueryString()
    {
        if (_nativeCookedUrl.pQueryString != null && _nativeCookedUrl.QueryStringLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pQueryString, _nativeCookedUrl.QueryStringLength / 2);
        }
        return null;
    }
}
