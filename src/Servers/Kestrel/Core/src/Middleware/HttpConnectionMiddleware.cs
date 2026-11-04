// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class HttpConnectionMiddleware<TContext> where TContext : notnull
{
    private readonly ServiceContext _serviceContext;
    private readonly IHttpApplication<TContext> _application;
    private readonly HttpProtocols _endpointDefaultProtocols;
    private readonly bool _addAltSvcHeader;

    public HttpConnectionMiddleware(ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols, bool addAltSvcHeader)
    {
        _serviceContext = serviceContext;
        _application = application;
        _endpointDefaultProtocols = protocols;
        _addAltSvcHeader = addAltSvcHeader;
    }

    public Task OnConnectionAsync(ConnectionContext connectionContext)
    {
        var memoryPoolFeature = connectionContext.Features.Get<IMemoryPoolFeature>();
        var protocols = connectionContext.Features.Get<HttpProtocolsFeature>()?.HttpProtocols ?? _endpointDefaultProtocols;
        var metricContext = connectionContext.Features.GetRequiredFeature<IConnectionMetricsContextFeature>().MetricsContext;
        var localEndPoint = connectionContext.LocalEndPoint as IPEndPoint;
        var altSvcHeader = _addAltSvcHeader && localEndPoint != null ? HttpUtilities.GetEndpointAltSvc(localEndPoint, protocols) : null;

        var httpConnectionContext = new HttpConnectionContext(
            connectionContext.ConnectionId,
            protocols,
            altSvcHeader,
            connectionContext,
            _serviceContext,
            connectionContext.Features,
            memoryPoolFeature?.MemoryPool ?? System.Buffers.MemoryPool<byte>.Shared,
            localEndPoint,
            connectionContext.RemoteEndPoint as IPEndPoint,
            metricContext);
        httpConnectionContext.Transport = connectionContext.Transport;

        var connection = new HttpConnection(httpConnectionContext);

        return connection.ProcessRequestsAsync(_application);
    }
}
