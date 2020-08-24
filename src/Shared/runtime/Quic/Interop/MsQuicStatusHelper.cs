// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicStatusHelper
    {
        internal static bool SuccessfulStatusCode(uint status)
        {
            if (OperatingSystem.IsWindows())
            {
                return status < 0x80000000;
            }

            if (OperatingSystem.IsLinux())
            {
                return (int)status <= 0;
            }

            return false;
        }
    }
}
