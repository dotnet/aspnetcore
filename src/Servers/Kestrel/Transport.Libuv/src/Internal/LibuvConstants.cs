// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal static class LibuvConstants
    {
        public const int EOF = -4095;
        public static readonly int? ECONNRESET = GetECONNRESET();
        public static readonly int? EADDRINUSE = GetEADDRINUSE();
        public static readonly int? ENOTSUP = GetENOTSUP();
        public static readonly int? EPIPE = GetEPIPE();
        public static readonly int? ECANCELED = GetECANCELED();
        public static readonly int? ENOTCONN = GetENOTCONN();
        public static readonly int? EINVAL = GetEINVAL();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConnectionReset(int errno)
        {
            return errno == ECONNRESET || errno == EPIPE || errno == ENOTCONN || errno == EINVAL;
        }

        private static int? GetECONNRESET()
        {
            if (OperatingSystem.IsWindows())
            {
                return -4077;
            }
            else if (OperatingSystem.IsLinux())
            {
                return -104;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -54;
            }
            return null;
        }

        private static int? GetEPIPE()
        {
            if (OperatingSystem.IsWindows())
            {
                return -4047;
            }
            else if (OperatingSystem.IsLinux())
            {
                return -32;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -32;
            }
            return null;
        }

        private static int? GetENOTCONN()
        {
            if (OperatingSystem.IsWindows())
            {
                return -4053;
            }
            else if (OperatingSystem.IsLinux())
            {
                return -107;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -57;
            }
            return null;
        }

        private static int? GetEINVAL()
        {
            if (OperatingSystem.IsWindows())
            {
                return -4071;
            }
            else if (OperatingSystem.IsLinux())
            {
                return -22;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -22;
            }
            return null;
        }

        private static int? GetEADDRINUSE()
        {
            if (OperatingSystem.IsWindows())
            {
                return -4091;
            }
            else if (OperatingSystem.IsLinux())
            {
                return -98;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -48;
            }
            return null;
        }

        private static int? GetENOTSUP()
        {
            if (OperatingSystem.IsLinux())
            {
                return -95;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -45;
            }
            return null;
        }

        private static int? GetECANCELED()
        {
            if (OperatingSystem.IsWindows())
            {
                return -4081;
            }
            else if (OperatingSystem.IsLinux())
            {
                return -125;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return -89;
            }
            return null;
        }
    }
}
