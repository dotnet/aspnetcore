// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.Cng;

internal static class OSVersionUtil
{
    private static readonly OSVersion _osVersion = GetOSVersion();

    private static OSVersion GetOSVersion()
    {
        if (Environment.OSVersion.Platform is not PlatformID.Win32NT)
        {
            // Not running on Win7+.
            return OSVersion.NotWindows;
        }

        try
        {
            // REVIEW: Should we just use the lazy handle instead of disposing?
            using var bcryptLibHandle = SafeLibraryHandle.Open(UnsafeNativeMethods.BCRYPT_LIB);

            if (bcryptLibHandle.DoesProcExist("BCryptKeyDerivation"))
            {
                // We're running on Win8+.
                return OSVersion.Win8OrLater;
            }
            else
            {
                // We're running on Win7+.
                return OSVersion.Win7OrLater;
            }
        }
        catch
        {
            // Not running on Win7+.
            return OSVersion.NotWindows;
        }
    }

    public static bool IsWindows()
    {
        return (_osVersion >= OSVersion.Win7OrLater);
    }

    public static bool IsWindows8OrLater()
    {
        return (_osVersion >= OSVersion.Win8OrLater);
    }

    private enum OSVersion
    {
        NotWindows = 0,
        Win7OrLater = 1,
        Win8OrLater = 2
    }
}
