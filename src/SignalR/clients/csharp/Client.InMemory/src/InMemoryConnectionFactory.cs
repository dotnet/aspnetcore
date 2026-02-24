// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.InMemory;

/// <summary>
/// An <see cref="IConnectionFactory"/> that creates in-memory connections backed by <see cref="System.IO.Pipelines.Pipe"/>
/// and dispatches the server-side end to the associated <see cref="InMemoryConnectionChannel{THub}"/>.
/// </summary>
/// <typeparam name="THub">The Hub type to connect to.</typeparam>
internal sealed partial class InMemoryConnectionFactory<THub> : IConnectionFactory where THub : class
{
    private readonly InMemoryConnectionChannel<THub> _channel;
    private readonly ILogger<InMemoryConnectionFactory<THub>> _logger;

    public InMemoryConnectionFactory(InMemoryConnectionChannel<THub> channel, ILogger<InMemoryConnectionFactory<THub>> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var pipeOptions = new PipeOptions(useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(pipeOptions, pipeOptions);

        var clientConnection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application);
        var serverConnection = new DefaultConnectionContext(clientConnection.ConnectionId, pair.Application, pair.Transport);

        Log.InMemoryConnectionStarting(_logger, clientConnection.ConnectionId);

        await _channel.Writer.WriteAsync(serverConnection, cancellationToken);

        return clientConnection;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "In-memory connection '{ConnectionId}' starting.", EventName = "InMemoryConnectionStarting")]
        public static partial void InMemoryConnectionStarting(ILogger logger, string connectionId);
    }
}
