// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
