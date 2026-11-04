// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Crankier.Server
{
    public class ConnectionSummary
    {
        public int TotalConnected { get; set; }

        public int TotalDisconnected { get; set; }

        public int PeakConnections { get; set; }

        public int CurrentConnections { get; set; }

        public int ReceivedCount { get; set; }
    }
}