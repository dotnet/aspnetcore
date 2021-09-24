// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.Cng
{
    internal static class OSVersionUtil
    {
        private static readonly OSVersion _osVersion = GetOSVersion();

        private static OSVersion GetOSVersion()
        {
            const string BCRYPT_LIB = "bcrypt.dll";
            SafeLibraryHandle? bcryptLibHandle = null;
            try
            {
                bcryptLibHandle = SafeLibraryHandle.Open(BCRYPT_LIB);
            }
            catch
            {
                // we'll handle the exceptional case later
            }

            if (bcryptLibHandle != null)
            {
                using (bcryptLibHandle)
                {
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
            }
            else
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
}
