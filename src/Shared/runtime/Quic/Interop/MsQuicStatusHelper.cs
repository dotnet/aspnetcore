// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
