// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal readonly struct TlsHandshakeMetricsContext
{
    public TlsHandshakeMetricsContext(BaseConnectionContext connectionContext, bool currentTlsHandshakesCounterEnabled)
    {
        ConnectionContext = connectionContext;
        CurrentTlsHandshakesCounterEnabled = currentTlsHandshakesCounterEnabled;
    }

    public BaseConnectionContext ConnectionContext { get; }

    public bool CurrentTlsHandshakesCounterEnabled { get; }
}
