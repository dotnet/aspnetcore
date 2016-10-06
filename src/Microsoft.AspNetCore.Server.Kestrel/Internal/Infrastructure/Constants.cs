// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    internal class Constants
    {
        public const int ListenBacklog = 128;

        public const int EOF = -4095;
        public static readonly int? EADDRINUSE = GetEADDRINUSE();

        /// <summary>
        /// Prefix of host name used to specify Unix sockets in the configuration.
        /// </summary>
        public const string UnixPipeHostPrefix = "unix:/";

        public const string ServerName = "Kestrel";

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
