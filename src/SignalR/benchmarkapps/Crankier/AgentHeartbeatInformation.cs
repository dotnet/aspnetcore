// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
