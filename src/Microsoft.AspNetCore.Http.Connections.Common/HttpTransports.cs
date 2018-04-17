// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Connections
{
    public static class HttpTransports
    {
        // Note that this is static readonly instead of const so it is not baked into a DLL when referenced
        // Updating package without recompiling will automatically pick up new transports added here
        public static readonly HttpTransportType All = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
    }
}
