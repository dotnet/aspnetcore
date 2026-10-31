// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class AgentHeartbeatInformation
    {
        public string HostName { get; set; }

        public string TargetAddress { get; set; }

        public int TotalConnectionsRequested { get; set; }

        public bool ApplyingLoad { get; set; }

        public List<WorkerHeartbeatInformation> Workers { get; set; }
    }
}
