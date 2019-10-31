// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal static class MsQuicStatusHelper
    {
        internal static bool Succeeded(uint status)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return status <= 0x80000000;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return (int)status <= 0;
            }

            return false;
        }
    }
}
