// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
