// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports
{
    internal static class CustomHeaderNames
    {
        /// <summary>
        /// Used as a response header to let other parties, like telemetry systems, know that the request/connection will take a long time.
        /// </summary>
        public static readonly string LongRunning = "Long-Running";
    }
}
