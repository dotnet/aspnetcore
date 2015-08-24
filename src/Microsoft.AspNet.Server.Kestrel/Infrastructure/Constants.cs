// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    internal class Constants
    {
        public const int ListenBacklog = 128;

        /// <summary>
        /// URL scheme for specifying Unix sockets in the configuration.
        /// </summary>
        public const string UnixScheme = "unix";
    }
}
