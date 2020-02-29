// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Specifies transports that the client can use to send HTTP requests.
    /// </summary>
    /// <remarks>
    /// This enumeration has a <see cref="FlagsAttribute"/> attribute that allows a bitwise combination of its member values.
    /// </remarks>
    [Flags]
    public enum HttpTransportType
    {
        /// <summary>
        /// Specifies that no transport is used.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies that the web sockets transport is used.
        /// </summary>
        WebSockets = 1,
        /// <summary>
        /// Specifies that the server sent events transport is used.
        /// </summary>
        ServerSentEvents = 2,
        /// <summary>
        /// Specifies that the long polling transport is used.
        /// </summary>
        LongPolling = 4,
    }
}
