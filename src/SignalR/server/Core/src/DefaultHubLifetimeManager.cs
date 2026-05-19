// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A default in-memory lifetime manager abstraction for <see cref="Hub"/> instances.
/// </summary>
public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
{
    private readonly HubConnectionStore _connections = new HubConnectionStore();
    private readonly HubGroupList _groups = new HubGroupList();
    private readonly ILogger _logger;
    private readonly ClientResultsManager _clientResultsManager = new();
    private ulong _lastInvocationId;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHubLifetimeManager{THub}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DefaultHubLifetimeManager(ILogger<DefaultHubLifetimeManager<THub>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(groupName);

        var connection = _connections[connectionId];
        if (connection == null)
        {
            return Task.CompletedTask;
        }

        // Track groups in the connection object
        lock (connection.GroupNames)
        {
            if (!connection.GroupNames.Add(groupName))
            {
                // Connection already in group
                return Task.CompletedTask;
            }

            _groups.Add(connection, groupName);
        }

        // Connection disconnected while adding to group, remove it in case the Add was called after OnDisconnectedAsync removed items from the group
        if (connection.ConnectionAborted.IsCancellationRequested)
        {
            _groups.Remove(connection.ConnectionId, groupName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(groupName);

        var connection = _connections[connectionId];
        if (connection == null)
        {
            return Task.CompletedTask;
        }

        // Remove from previously saved groups
        lock (connection.GroupNames)
        {
            if (!connection.GroupNames.Remove(groupName))
            {
                // Connection not in group
                return Task.CompletedTask;
            }

            _groups.Remove(connectionId, groupName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        return SendToAllConnections(methodName, args, include: null, state: null, cancellationToken);
    }

    private Task SendToAllConnections(string methodName, object?[] args, Func<HubConnectionContext, object?, bool>? include, object? state = null, CancellationToken cancellationToken = default)
    {
        List<Task>? tasks = null;
        SerializedHubMessage? message = null;

        // foreach over HubConnectionStore avoids allocating an enumerator
        foreach (var connection in _connections)
        {
            if (include != null && !include(connection, state))
            {
                continue;
            }

            if (message == null)
            {
                message = DefaultHubLifetimeManager<THub>.CreateSerializedInvocationMessage(methodName, args);
            }

            var task = connection.WriteAsync(message, cancellationToken);

            if (!task.IsCompletedSuccessfully)
            {
                if (tasks == null)
                {
                    tasks = new List<Task>();
                }

                tasks.Add(task.AsTask());
            }
            else
            {
                // If it's a IValueTaskSource backed ValueTask,
                // inform it its result has been read so it can reset
                task.GetAwaiter().GetResult();
            }
        }

        if (tasks == null)
        {
            return Task.CompletedTask;
        }

        // Some connections are slow
        return Task.WhenAll(tasks);
    }

    // Tasks and message are passed by ref so they can be lazily created inside the method post-filtering,
    // while still being re-usable when sending to multiple groups
    private static void SendToGroupConnections(string methodName, object?[] args, ConcurrentDictionary<string, HubConnectionContext> connections, Func<HubConnectionContext, object?, bool>? include, object? state, ref List<Task>? tasks, ref SerializedHubMessage? message, CancellationToken cancellationToken)
    {
        // foreach over ConcurrentDictionary avoids allocating an enumerator
        foreach (var connection in connections)
        {
            if (include != null && !include(connection.Value, state))
            {
                continue;
            }

            if (message == null)
            {
                message = DefaultHubLifetimeManager<THub>.CreateSerializedInvocationMessage(methodName, args);
            }

            var task = connection.Value.WriteAsync(message, cancellationToken);

            if (!task.IsCompletedSuccessfully)
            {
                if (tasks == null)
                {
                    tasks = new List<Task>();
                }

                tasks.Add(task.AsTask());
            }
            else
            {
                // If it's a IValueTaskSource backed ValueTask,
                // inform it its result has been read so it can reset
                task.GetAwaiter().GetResult();
            }
        }
    }

    /// <inheritdoc />
    public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        var connection = _connections[connectionId];

        if (connection == null)
        {
            return Task.CompletedTask;
        }

        // We're sending to a single connection
        // Write message directly to connection without caching it in memory
        var message = CreateInvocationMessage(methodName, args);

        return connection.WriteAsync(message, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public override Task SendGroupAsync(string groupName, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        var group = _groups[groupName];
        if (group != null)
        {
            // Can't optimize for sending to a single connection in a group because
            // group might be modified inbetween checking and sending
            List<Task>? tasks = null;
            SerializedHubMessage? message = null;
            DefaultHubLifetimeManager<THub>.SendToGroupConnections(methodName, args, group, null, null, ref tasks, ref message, cancellationToken);

            if (tasks != null)
            {
                return Task.WhenAll(tasks);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        // Each task represents the list of tasks for each of the writes within a group
        List<Task>? tasks = null;
        SerializedHubMessage? message = null;

        foreach (var groupName in groupNames)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new InvalidOperationException("Cannot send to an empty group name.");
            }

            var group = _groups[groupName];
            if (group != null)
            {
                DefaultHubLifetimeManager<THub>.SendToGroupConnections(methodName, args, group, null, null, ref tasks, ref message, cancellationToken);
            }
        }

        if (tasks != null)
        {
            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        var group = _groups[groupName];
        if (group != null)
        {
            List<Task>? tasks = null;
            SerializedHubMessage? message = null;

            DefaultHubLifetimeManager<THub>.SendToGroupConnections(methodName, args, group, (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds, ref tasks, ref message, cancellationToken);

            if (tasks != null)
            {
                return Task.WhenAll(tasks);
            }
        }

        return Task.CompletedTask;
    }

    private static SerializedHubMessage CreateSerializedInvocationMessage(string methodName, object?[] args)
    {
        return new SerializedHubMessage(CreateInvocationMessage(methodName, args));
    }

    private static HubMessage CreateInvocationMessage(string methodName, object?[] args)
    {
        return new InvocationMessage(methodName, args);
    }

    /// <inheritdoc />
    public override Task SendUserAsync(string userId, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        return SendToAllConnections(methodName, args, (connection, state) => string.Equals(connection.UserIdentifier, (string)state!, StringComparison.Ordinal), userId, cancellationToken);
    }

    /// <inheritdoc />
    public override Task OnConnectedAsync(HubConnectionContext connection)
    {
        _connections.Add(connection);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        lock (connection.GroupNames)
        {
            // Remove from tracked groups one by one
            foreach (var groupName in connection.GroupNames)
            {
                _groups.Remove(connection.ConnectionId, groupName);
            }
        }

        _connections.Remove(connection);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
    {
        return SendToAllConnections(methodName, args, (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds, cancellationToken);
    }

    /// <inheritdoc />
    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        return SendToAllConnections(methodName, args, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
    }

    /// <inheritdoc />
    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        return SendToAllConnections(methodName, args, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.UserIdentifier), userIds, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<T> InvokeConnectionAsync<T>(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        var connection = _connections[connectionId];

        if (connection == null)
        {
            throw new IOException($"Connection '{connectionId}' does not exist.");
        }

        var id = Interlocked.Increment(ref _lastInvocationId);
        // prefix the client result ID with 's' for server, so that it won't conflict with other CompletionMessage's from the client
        // e.g. Stream IDs when completing
        var invocationId = $"s{id}";

        using var _ = CancellationTokenUtils.CreateLinkedToken(cancellationToken,
            connection.ConnectionAborted, out var linkedToken);
        var task = _clientResultsManager.AddInvocation<T>(connectionId, invocationId, linkedToken);

        try
        {
            // We're sending to a single connection
            // Write message directly to connection without caching it in memory
            var message = new InvocationMessage(invocationId, methodName, args);

            await connection.WriteAsync(message, cancellationToken);
        }
        catch
        {
            _clientResultsManager.RemoveInvocation(invocationId);
            throw;
        }

        try
        {
            return await task;
        }
        catch
        {
            // ConnectionAborted will trigger a generic "Canceled" exception from the task, let's convert it into a more specific message.
            if (connection.ConnectionAborted.IsCancellationRequested)
            {
                throw new IOException($"Connection '{connectionId}' disconnected.");
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public override Task SetConnectionResultAsync(string connectionId, CompletionMessage result)
    {
        _clientResultsManager.TryCompleteResult(connectionId, result);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override bool TryGetReturnType(string invocationId, [NotNullWhen(true)] out Type? type)
    {
        if (_clientResultsManager.TryGetType(invocationId, out type))
        {
            return true;
        }
        type = null;
        return false;
    }
}
