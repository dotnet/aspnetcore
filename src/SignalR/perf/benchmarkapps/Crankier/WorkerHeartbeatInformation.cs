// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class WorkerHeartbeatInformation
    {
        public int Id { get; set; }

        public int ConnectedCount { get; set; }

        public int DisconnectedCount { get; set; }

        public int ReconnectingCount { get; set; }

        public int TargetConnectionCount { get; set; }
    }
}
