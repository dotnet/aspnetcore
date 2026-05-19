// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
