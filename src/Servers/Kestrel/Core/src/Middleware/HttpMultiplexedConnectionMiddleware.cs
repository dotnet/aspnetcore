// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class HttpMultiplexedConnectionMiddleware<TContext> where TContext : notnull
{
    private readonly ServiceContext _serviceContext;
    private readonly IHttpApplication<TContext> _application;
    private readonly HttpProtocols _protocols;
    private readonly bool _addAltSvcHeader;

    public HttpMultiplexedConnectionMiddleware(ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols, bool addAltSvcHeader)
    {
        _serviceContext = serviceContext;
        _application = application;
        _protocols = protocols;
        _addAltSvcHeader = addAltSvcHeader;
    }

    public Task OnConnectionAsync(MultiplexedConnectionContext connectionContext)
    {
        var memoryPoolFeature = connectionContext.Features.Get<IMemoryPoolFeature>();
        var localEndPoint = connectionContext.LocalEndPoint as IPEndPoint;
        var altSvcHeader = _addAltSvcHeader && localEndPoint != null ? HttpUtilities.GetEndpointAltSvc(localEndPoint, _protocols) : null;
        var metricContext = connectionContext.Features.GetRequiredFeature<IConnectionMetricsContextFeature>().MetricsContext;

        var httpConnectionContext = new HttpMultiplexedConnectionContext(
            connectionContext.ConnectionId,
            _protocols,
            altSvcHeader,
            connectionContext,
            _serviceContext,
            connectionContext.Features,
            memoryPoolFeature?.MemoryPool ?? System.Buffers.MemoryPool<byte>.Shared,
            localEndPoint,
            connectionContext.RemoteEndPoint as IPEndPoint,
            metricContext);

        if (connectionContext.Features.Get<IConnectionMetricsTagsFeature>() is { } metricsTags)
        {
            // HTTP/3 is always TLS 1.3. If multiple versions are support in the future then this value will need to be detected.
            metricsTags.Tags.Add(new KeyValuePair<string, object?>("tls.protocol.version", "1.3"));
        }

        var connection = new HttpConnection(httpConnectionContext);

        return connection.ProcessRequestsAsync(_application);
    }
}
