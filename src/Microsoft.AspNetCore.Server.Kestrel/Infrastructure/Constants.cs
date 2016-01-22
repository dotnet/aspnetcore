// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    internal class Constants
    {
        public const int ListenBacklog = 128;

        public const int EOF = -4095;
        public const int ECONNRESET = -4077;

        /// <summary>
        /// Prefix of host name used to specify Unix sockets in the configuration.
        /// </summary>
        public const string UnixPipeHostPrefix = "unix:/";

        /// <summary>
        /// DateTime format string for RFC1123. See  https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#RFC1123
        /// for info on the format.
        /// </summary>
        public const string RFC1123DateFormat = "r";
    }
}
