// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Quic;
using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;

/// <summary>
/// Listens for new Quic Connections.
/// </summary>
internal sealed class QuicConnectionListener : IMultiplexedConnectionListener, IAsyncDisposable
{
    private readonly ILogger _log;
    private bool _disposed;
    private readonly QuicTransportContext _context;
    private QuicListener? _listener;
    private readonly QuicListenerOptions _quicListenerOptions;

    public QuicConnectionListener(QuicTransportOptions options, ILogger log, EndPoint endpoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
    {
        if (!QuicListener.IsSupported)
        {
            throw new NotSupportedException("QUIC is not supported or enabled on this platform. See https://aka.ms/aspnet/kestrel/http3reqs for details.");
        }

        _log = log;
        _context = new QuicTransportContext(_log, options);
        _quicListenerOptions = new();

        if (endpoint is not IPEndPoint listenEndPoint)
        {
            throw new InvalidOperationException($"QUIC doesn't support listening on the configured endpoint type. Expected {nameof(IPEndPoint)} but got {endpoint.GetType().Name}.");
        }

        _quicListenerOptions.ServerAuthenticationOptions = sslServerAuthenticationOptions;
        _quicListenerOptions.ListenEndPoint = listenEndPoint;
        _quicListenerOptions.IdleTimeout = options.IdleTimeout;
        _quicListenerOptions.MaxBidirectionalStreams = options.MaxBidirectionalStreamCount;
        _quicListenerOptions.MaxUnidirectionalStreams = options.MaxUnidirectionalStreamCount;
        _quicListenerOptions.ListenBacklog = options.Backlog;

        // Setting to listenEndPoint to prevent the property from being null.
        // This will be initialized when CreateListenerAsync() is invoked.
        EndPoint = listenEndPoint;
    }

    public EndPoint EndPoint { get; set; }

    public async ValueTask CreateListenerAsync()
    {
        _listener = await QuicListener.ListenAsync(_quicListenerOptions);

        // Listener endpoint will resolve an ephemeral port, e.g. 127.0.0.1:0, into the actual port.
        EndPoint = _listener.ListenEndPoint;
    }

    public async ValueTask<MultiplexedConnectionContext?> AcceptAsync(IFeatureCollection? features = null, CancellationToken cancellationToken = default)
    {
        if (_listener == null)
        {
            throw new InvalidOperationException($"The listener needs to be initialized by calling {nameof(CreateListenerAsync)}.");
        }

        try
        {
            var quicConnection = await _listener.AcceptConnectionAsync(cancellationToken);
            var connectionContext = new QuicConnectionContext(quicConnection, _context);

            QuicLog.AcceptedConnection(_log, connectionContext);

            return connectionContext;
        }
        catch (QuicOperationAbortedException ex)
        {
            _log.LogDebug("Listener has aborted with exception: {Message}", ex.Message);
        }
        return null;
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        await DisposeAsync();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _listener?.Dispose();
        _disposed = true;

        return ValueTask.CompletedTask;
    }
}
