// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Constants related to HTTP transports.
    /// </summary>
    public static class HttpTransports
    {
        /// <summary>
        /// A bitmask combining all available <see cref="HttpTransportType"/> values.
        /// </summary>
        public static readonly HttpTransportType All = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
    }
}
