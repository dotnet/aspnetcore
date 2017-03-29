// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    internal static class Constants
    {
        public const int ListenBacklog = 128;

        public const int EOF = -4095;
        public static readonly int? ECONNRESET = GetECONNRESET();
        public static readonly int? EADDRINUSE = GetEADDRINUSE();

        private static int? GetECONNRESET()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return -4077;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return -104;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return -54;
            }
            return null;
        }

        private static int? GetEADDRINUSE()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return -4091;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return -98;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return -48;
            }
            return null;
        }
    }
}
