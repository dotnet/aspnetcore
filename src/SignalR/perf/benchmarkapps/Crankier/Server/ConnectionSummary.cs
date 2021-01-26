// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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