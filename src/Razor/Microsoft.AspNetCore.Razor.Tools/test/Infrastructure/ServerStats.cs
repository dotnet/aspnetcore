// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal struct ServerStats
    {
        internal readonly int Connections;
        internal readonly int CompletedConnections;

        internal ServerStats(int connections, int completedConnections)
        {
            Connections = connections;
            CompletedConnections = completedConnections;
        }
    }
}
