// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.HttpSys.Internal;

// Note this type should only be used while the request buffer remains pinned
internal readonly struct CookedUrl
{
    private readonly HTTP_COOKED_URL _nativeCookedUrl;

    internal CookedUrl(HTTP_COOKED_URL nativeCookedUrl)
    {
        _nativeCookedUrl = nativeCookedUrl;
    }

    internal unsafe string? GetFullUrl()
    {
        if (!_nativeCookedUrl.pFullUrl.Equals(null) && _nativeCookedUrl.FullUrlLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pFullUrl.Value, _nativeCookedUrl.FullUrlLength / 2);
        }
        return null;
    }

    internal unsafe string? GetHost()
    {
        if (!_nativeCookedUrl.pHost.Equals(null) && _nativeCookedUrl.HostLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pHost.Value, _nativeCookedUrl.HostLength / 2);
        }
        return null;
    }

    internal unsafe string? GetAbsPath()
    {
        if (!_nativeCookedUrl.pAbsPath.Equals(null) && _nativeCookedUrl.AbsPathLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pAbsPath.Value, _nativeCookedUrl.AbsPathLength / 2);
        }
        return null;
    }

    internal unsafe string? GetQueryString()
    {
        if (!_nativeCookedUrl.pQueryString.Equals(null) && _nativeCookedUrl.QueryStringLength > 0)
        {
            return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pQueryString.Value, _nativeCookedUrl.QueryStringLength / 2);
        }
        return null;
    }
}
