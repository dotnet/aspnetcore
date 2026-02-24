// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.InMemory;

/// <summary>
/// A hosted service that reads from <see cref="InMemoryConnectionChannel{THub}"/> and dispatches
/// each incoming connection to the <see cref="ConnectionHandler"/> registered for <typeparamref name="THub"/>.
/// </summary>
/// <typeparam name="THub">The Hub type to handle connections for.</typeparam>
internal sealed partial class InMemoryHubConnectionDispatcher<THub> : BackgroundService where THub : class
{
    private readonly InMemoryConnectionChannel<THub> _channel;
    private readonly ConnectionHandler _connectionHandler;
    private readonly ILogger<InMemoryHubConnectionDispatcher<THub>> _logger;

    public InMemoryHubConnectionDispatcher(
        InMemoryConnectionChannel<THub> channel,
        IServiceProvider serviceProvider,
        ILogger<InMemoryHubConnectionDispatcher<THub>> logger)
    {
        _channel = channel;
        _connectionHandler = (ConnectionHandler)serviceProvider.GetRequiredService(channel.ConnectionHandlerType);
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.InMemoryDispatcherStarted(_logger, typeof(THub).Name);

        try
        {
            await foreach (var connection in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // Fire and forget each connection so the dispatcher can continue accepting new ones.
                _ = HandleConnectionAsync(connection);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }

        Log.InMemoryDispatcherStopped(_logger, typeof(THub).Name);
    }

    private async Task HandleConnectionAsync(ConnectionContext connection)
    {
        try
        {
            Log.InMemoryConnectionAccepted(_logger, connection.ConnectionId, typeof(THub).Name);
            await _connectionHandler.OnConnectedAsync(connection);
        }
        catch (Exception ex)
        {
            Log.InMemoryConnectionError(_logger, connection.ConnectionId, typeof(THub).Name, ex);
        }
        finally
        {
            await connection.DisposeAsync();
            Log.InMemoryConnectionDisposed(_logger, connection.ConnectionId, typeof(THub).Name);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "In-memory connection dispatcher started for hub '{HubName}'.", EventName = "InMemoryDispatcherStarted")]
        public static partial void InMemoryDispatcherStarted(ILogger logger, string hubName);

        [LoggerMessage(2, LogLevel.Information, "In-memory connection dispatcher stopped for hub '{HubName}'.", EventName = "InMemoryDispatcherStopped")]
        public static partial void InMemoryDispatcherStopped(ILogger logger, string hubName);

        [LoggerMessage(3, LogLevel.Debug, "In-memory connection '{ConnectionId}' accepted for hub '{HubName}'.", EventName = "InMemoryConnectionAccepted")]
        public static partial void InMemoryConnectionAccepted(ILogger logger, string connectionId, string hubName);

        [LoggerMessage(4, LogLevel.Error, "In-memory connection '{ConnectionId}' for hub '{HubName}' terminated with an error.", EventName = "InMemoryConnectionError")]
        public static partial void InMemoryConnectionError(ILogger logger, string connectionId, string hubName, Exception exception);

        [LoggerMessage(5, LogLevel.Debug, "In-memory connection '{ConnectionId}' for hub '{HubName}' disposed.", EventName = "InMemoryConnectionDisposed")]
        public static partial void InMemoryConnectionDisposed(ILogger logger, string connectionId, string hubName);
    }
}
