// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    internal class Constants
    {
        public const int ListenBacklog = 128;

        public const int EOF = -4095;
        public static readonly int? ECONNRESET = GetECONNRESET();
        public static readonly int? EADDRINUSE = GetEADDRINUSE();

        /// <summary>
        /// Prefix of host name used to specify Unix sockets in the configuration.
        /// </summary>
        public const string UnixPipeHostPrefix = "unix:/";

        /// <summary>
        /// DateTime format string for RFC1123. See  https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#RFC1123
        /// for info on the format.
        /// </summary>
        public const string RFC1123DateFormat = "r";

        public const string ServerName = "Kestrel";

        private static int? GetECONNRESET()
        {
            switch (PlatformServices.Default.Runtime.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    return -4077;
                case Platform.Linux:
                    return -104;
                case Platform.Darwin:
                    return -54;
                default:
                    return null;
            }
        }

        private static int? GetEADDRINUSE()
        {
            switch (PlatformServices.Default.Runtime.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    return -4091;
                case Platform.Linux:
                    return -98;
                case Platform.Darwin:
                    return -48;
                default:
                    return null;
            }
        }
    }
}
