// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2Stream<TContext> : Http2Stream, IHostContextContainer<TContext> where TContext : notnull
{
    private readonly IHttpApplication<TContext> _application;

    public Http2Stream(IHttpApplication<TContext> application, Http2StreamContext context)
    {
        Initialize(context);
        _application = application;
    }

    public override void Execute()
    {
        KestrelEventSource.Log.RequestQueuedStop(this, AspNetCore.Http.HttpProtocol.Http2);
        ServiceContext.Metrics.RequestQueuedStop(MetricsContext, KestrelMetrics.Http2);

        // REVIEW: Should we store this in a field for easy debugging?
        _ = ProcessRequestsAsync(_application);
    }

    // Pooled Host context
    TContext? IHostContextContainer<TContext>.HostContext { get; set; }
}
