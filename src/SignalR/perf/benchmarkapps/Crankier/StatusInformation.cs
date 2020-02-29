// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class StatusInformation
    {
        public int ConnectingCount { get; set; }
        public int ConnectedCount { get; set; }
        public int DisconnectedCount { get; set; }
        public int ReconnectingCount { get; set; }
        public int FaultedCount { get; set; }
        public int TargetConnectionCount { get; set; }


        // Set by agent.
        public int PeakConnections { get; set;}

        public StatusInformation Add(StatusInformation value)
        {
            return new StatusInformation()
            {
                ConnectingCount = ConnectingCount + value.ConnectingCount,
                ConnectedCount = ConnectedCount + value.ConnectedCount,
                DisconnectedCount = DisconnectedCount + value.DisconnectedCount,
                ReconnectingCount = ReconnectingCount + value.ReconnectingCount,
                FaultedCount = FaultedCount + value.FaultedCount,
                TargetConnectionCount = TargetConnectionCount + value.TargetConnectionCount,
            };
        }
    }
}
