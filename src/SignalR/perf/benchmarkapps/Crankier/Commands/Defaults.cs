// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Crankier.Commands
{
    internal static class Defaults
    {
        public static readonly int NumberOfWorkers = 1;
        public static readonly int NumberOfConnections = 10_000;
        public static readonly int SendDurationInSeconds = 300;
        public static readonly HttpTransportType TransportType = HttpTransportType.WebSockets;
        public static readonly LogLevel LogLevel = LogLevel.None;
    }
}
