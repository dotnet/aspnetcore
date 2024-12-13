// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class ConnectionMetricsContext
{
    public required BaseConnectionContext ConnectionContext { get; init; }
    public bool CurrentConnectionsCounterEnabled { get; init; }
    public bool ConnectionDurationEnabled { get; init; }
    public bool QueuedConnectionsCounterEnabled { get; init; }
    public bool QueuedRequestsCounterEnabled { get; init; }
    public bool CurrentUpgradedRequestsCounterEnabled { get; init; }
    public bool CurrentTlsHandshakesCounterEnabled { get; init; }

    public ConnectionEndReason? ConnectionEndReason { get; set; }
}
