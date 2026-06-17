// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class UserProxy<THub> : IClientProxy where THub : Hub
{
    private readonly string _userId;
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public UserProxy(HubLifetimeManager<THub> lifetimeManager, string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        _lifetimeManager = lifetimeManager;
        _userId = userId;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendUserAsync(_userId, method, args, cancellationToken);
    }
}

internal sealed class MultipleUserProxy<THub> : IClientProxy where THub : Hub
{
    private readonly IReadOnlyList<string> _userIds;
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public MultipleUserProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> userIds)
    {
        _lifetimeManager = lifetimeManager;
        _userIds = userIds;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendUsersAsync(_userIds, method, args, cancellationToken);
    }
}

internal sealed class GroupProxy<THub> : IClientProxy where THub : Hub
{
    private readonly string _groupName;
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public GroupProxy(HubLifetimeManager<THub> lifetimeManager, string groupName)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        _lifetimeManager = lifetimeManager;
        _groupName = groupName;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendGroupAsync(_groupName, method, args, cancellationToken);
    }
}

internal sealed class MultipleGroupProxy<THub> : IClientProxy where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;
    private readonly IReadOnlyList<string> _groupNames;

    public MultipleGroupProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> groupNames)
    {
        _lifetimeManager = lifetimeManager;
        _groupNames = groupNames;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendGroupsAsync(_groupNames, method, args, cancellationToken);
    }
}

internal sealed class GroupExceptProxy<THub> : IClientProxy where THub : Hub
{
    private readonly string _groupName;
    private readonly HubLifetimeManager<THub> _lifetimeManager;
    private readonly IReadOnlyList<string> _excludedConnectionIds;

    public GroupExceptProxy(HubLifetimeManager<THub> lifetimeManager, string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        _lifetimeManager = lifetimeManager;
        _groupName = groupName;
        _excludedConnectionIds = excludedConnectionIds;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendGroupExceptAsync(_groupName, method, args, _excludedConnectionIds, cancellationToken);
    }
}

internal sealed class AllClientProxy<THub> : IClientProxy where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public AllClientProxy(HubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendAllAsync(method, args, cancellationToken);
    }
}

internal sealed class AllClientsExceptProxy<THub> : IClientProxy where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;
    private readonly IReadOnlyList<string> _excludedConnectionIds;

    public AllClientsExceptProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> excludedConnectionIds)
    {
        _lifetimeManager = lifetimeManager;
        _excludedConnectionIds = excludedConnectionIds;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendAllExceptAsync(method, args, _excludedConnectionIds, cancellationToken);
    }
}

internal sealed class MultipleClientProxy<THub> : IClientProxy where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;
    private readonly IReadOnlyList<string> _connectionIds;

    public MultipleClientProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> connectionIds)
    {
        _lifetimeManager = lifetimeManager;
        _connectionIds = connectionIds;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendConnectionsAsync(_connectionIds, method, args, cancellationToken);
    }
}

internal sealed class SingleClientProxy<THub> : ISingleClientProxy where THub : Hub
{
    private readonly string _connectionId;
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public SingleClientProxy(HubLifetimeManager<THub> lifetimeManager, string connectionId)
    {
        _lifetimeManager = lifetimeManager;
        ArgumentNullException.ThrowIfNull(connectionId);
        _connectionId = connectionId;
    }

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.SendConnectionAsync(_connectionId, method, args, cancellationToken);
    }

    public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        return _lifetimeManager.InvokeConnectionAsync<T>(_connectionId, method, args ?? Array.Empty<object?>(), cancellationToken);
    }
}
