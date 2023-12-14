// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal readonly struct ConnectionMetricsContext
{
    public BaseConnectionContext ConnectionContext { get; }
    public bool CurrentConnectionsCounterEnabled { get; }
    public bool ConnectionDurationEnabled { get; }
    public bool QueuedConnectionsCounterEnabled { get; }
    public bool QueuedRequestsCounterEnabled { get; }
    public bool CurrentUpgradedRequestsCounterEnabled { get; }
    public bool CurrentTlsHandshakesCounterEnabled { get; }

    public ConnectionMetricsContext(BaseConnectionContext connectionContext, bool currentConnectionsCounterEnabled,
        bool connectionDurationEnabled, bool queuedConnectionsCounterEnabled, bool queuedRequestsCounterEnabled,
        bool currentUpgradedRequestsCounterEnabled, bool currentTlsHandshakesCounterEnabled)
    {
        ConnectionContext = connectionContext;
        CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
        ConnectionDurationEnabled = connectionDurationEnabled;
        QueuedConnectionsCounterEnabled = queuedConnectionsCounterEnabled;
        QueuedRequestsCounterEnabled = queuedRequestsCounterEnabled;
        CurrentUpgradedRequestsCounterEnabled = currentUpgradedRequestsCounterEnabled;
        CurrentTlsHandshakesCounterEnabled = currentTlsHandshakesCounterEnabled;
    }
}
