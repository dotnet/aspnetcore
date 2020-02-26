// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicStatusHelper
    {
        internal static bool SuccessfulStatusCode(uint status)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return status < 0x80000000;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return (int)status <= 0;
            }

            return false;
        }
    }
}
