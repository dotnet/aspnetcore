// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class LoggingMultiplexedConnectionMiddleware
{
    private readonly MultiplexedConnectionDelegate _multiplexedNext;
    private readonly ILogger _logger;

    public LoggingMultiplexedConnectionMiddleware(MultiplexedConnectionDelegate multiplexedNext, ILogger logger)
    {
        _multiplexedNext = multiplexedNext ?? throw new ArgumentNullException(nameof(multiplexedNext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task OnConnectionAsync(MultiplexedConnectionContext context)
    {
        return _multiplexedNext(new LoggingMultiplexedConnectionContext(context, _logger));
    }

    /// <summary>
    /// Wrap the initial <see cref="MultiplexedConnectionContext"/>.
    /// ConnectionContext's returned from ConnectAsync and AcceptAsync will then be wrapped.
    /// </summary>
    private sealed class LoggingMultiplexedConnectionContext : MultiplexedConnectionContext
    {
        private readonly MultiplexedConnectionContext _inner;
        private readonly ILogger _logger;

        public LoggingMultiplexedConnectionContext(MultiplexedConnectionContext inner, ILogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public override string ConnectionId { get => _inner.ConnectionId; set => _inner.ConnectionId = value; }
        public override IFeatureCollection Features => _inner.Features;
        public override IDictionary<object, object?> Items { get => _inner.Items; set => _inner.Items = value; }

        public override void Abort()
        {
            _inner.Abort();
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _inner.Abort(abortReason);
        }

        public override async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            var context = await _inner.AcceptAsync(cancellationToken);
            if (context != null)
            {
                context = new LoggingConnectionContext(context, _logger);
            }
            return context;
        }

        public override async ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection? features = null, CancellationToken cancellationToken = default)
        {
            var context = await _inner.ConnectAsync(features, cancellationToken);
            context = new LoggingConnectionContext(context, _logger);
            return context;
        }

        public override CancellationToken ConnectionClosed { get => _inner.ConnectionClosed; set => _inner.ConnectionClosed = value; }
        public override ValueTask DisposeAsync()
        {
            return _inner.DisposeAsync();
        }
        public override EndPoint? LocalEndPoint { get => _inner.LocalEndPoint; set => _inner.LocalEndPoint = value; }
        public override EndPoint? RemoteEndPoint { get => _inner.RemoteEndPoint; set => _inner.RemoteEndPoint = value; }
    }

    /// <summary>
    /// Wraps transport with <see cref="LoggingDuplexPipe"/>.
    /// </summary>
    private sealed class LoggingConnectionContext : ConnectionContext
    {
        private readonly ConnectionContext _inner;
        private readonly ILogger _logger;
        private readonly LoggingDuplexPipe _loggingPipe;

        public LoggingConnectionContext(ConnectionContext inner, ILogger logger)
        {
            _inner = inner;
            _logger = logger;

            _loggingPipe = new LoggingDuplexPipe(_inner.Transport, _logger);

            Transport = _loggingPipe;
        }

        public override void Abort()
        {
            _inner.Abort();
        }
        public override void Abort(ConnectionAbortedException abortReason)
        {
            _inner.Abort(abortReason);
        }

        public override CancellationToken ConnectionClosed { get => _inner.ConnectionClosed; set => _inner.ConnectionClosed = value; }

        public override string ConnectionId { get => _inner.ConnectionId; set => _inner.ConnectionId = value; }

        public override async ValueTask DisposeAsync()
        {
            await _loggingPipe.DisposeAsync();
            await _inner.DisposeAsync();
        }

        public override IFeatureCollection Features => _inner.Features;

        public override IDictionary<object, object?> Items { get => _inner.Items; set => _inner.Items = value; }

        public override EndPoint? LocalEndPoint { get => _inner.LocalEndPoint; set => _inner.LocalEndPoint = value; }
        public override EndPoint? RemoteEndPoint { get => _inner.RemoteEndPoint; set => _inner.RemoteEndPoint = value; }

        public override IDuplexPipe Transport { get => _inner.Transport; set => _inner.Transport = value; }
    }
}
