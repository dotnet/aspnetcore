// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
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
        /// Prefix of host name used to specify pipe file descriptor in the configuration.
        /// </summary>
        public const string PipeDescriptorPrefix = "pipefd:";

        /// <summary>
        /// Prefix of host name used to specify socket descriptor in the configuration.
        /// </summary>
        public const string SocketDescriptorPrefix = "sockfd:";

        public const string ServerName = "Kestrel";

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
