// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static class Constants
    {
        public const int MaxExceptionDetailSize = 128;

        /// <summary>
        /// The endpoint Kestrel will bind to if nothing else is specified.
        /// </summary>
        public static readonly string DefaultServerAddress = "http://localhost:5000";

        /// <summary>
        /// The endpoint Kestrel will bind to if nothing else is specified and a default certificate is available.
        /// </summary>
        public static readonly string DefaultServerHttpsAddress = "https://localhost:5001";

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

        public static readonly TimeSpan RequestBodyDrainTimeout = TimeSpan.FromSeconds(5);
    }
}
