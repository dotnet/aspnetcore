// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubCallerClients : IHubCallerClients
{
    private readonly string _connectionId;
    private readonly IHubClients _hubClients;
    private readonly string[] _currentConnectionId;
    private readonly bool _parallelEnabled;

    // Client results don't work in OnConnectedAsync
    // This property is set by the hub dispatcher when those methods are being called
    // so we can prevent users from making blocking client calls by returning a custom ISingleClientProxy instance
    internal bool InvokeAllowed { get; set; }

    public HubCallerClients(IHubClients hubClients, string connectionId, bool parallelEnabled)
    {
        _connectionId = connectionId;
        _hubClients = hubClients;
        _currentConnectionId = new[] { _connectionId };
        _parallelEnabled = parallelEnabled;
    }

    IClientProxy IHubCallerClients<IClientProxy>.Caller => Caller;
    public ISingleClientProxy Caller
    {
        get
        {
            if (!_parallelEnabled)
            {
                return new NotParallelSingleClientProxy(_hubClients.Client(_connectionId));
            }
            if (!InvokeAllowed)
            {
                return new NoInvokeSingleClientProxy(_hubClients.Client(_connectionId));
            }
            return _hubClients.Client(_connectionId);
        }
    }

    public IClientProxy Others => _hubClients.AllExcept(_currentConnectionId);

    public IClientProxy All => _hubClients.All;

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
    {
        return _hubClients.AllExcept(excludedConnectionIds);
    }

    IClientProxy IHubClients<IClientProxy>.Client(string connectionId) => Client(connectionId);
    public ISingleClientProxy Client(string connectionId)
    {
        if (!_parallelEnabled)
        {
            return new NotParallelSingleClientProxy(_hubClients.Client(connectionId));
        }
        if (!InvokeAllowed)
        {
            return new NoInvokeSingleClientProxy(_hubClients.Client(_connectionId));
        }
        return _hubClients.Client(connectionId);
    }

    public IClientProxy Group(string groupName)
    {
        return _hubClients.Group(groupName);
    }

    public IClientProxy Groups(IReadOnlyList<string> groupNames)
    {
        return _hubClients.Groups(groupNames);
    }

    public IClientProxy OthersInGroup(string groupName)
    {
        return _hubClients.GroupExcept(groupName, _currentConnectionId);
    }

    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        return _hubClients.GroupExcept(groupName, excludedConnectionIds);
    }

    public IClientProxy User(string userId)
    {
        return _hubClients.User(userId);
    }

    public IClientProxy Clients(IReadOnlyList<string> connectionIds)
    {
        return _hubClients.Clients(connectionIds);
    }

    public IClientProxy Users(IReadOnlyList<string> userIds)
    {
        return _hubClients.Users(userIds);
    }

    private sealed class NotParallelSingleClientProxy : ISingleClientProxy
    {
        private readonly ISingleClientProxy _proxy;

        public NotParallelSingleClientProxy(ISingleClientProxy hubClients)
        {
            _proxy = hubClients;
        }

        public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Client results inside a Hub method requires HubOptions.MaximumParallelInvocationsPerClient to be greater than 1.");
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _proxy.SendCoreAsync(method, args, cancellationToken);
        }
    }

    private sealed class NoInvokeSingleClientProxy : ISingleClientProxy
    {
        private readonly ISingleClientProxy _proxy;

        public NoInvokeSingleClientProxy(ISingleClientProxy hubClients)
        {
            _proxy = hubClients;
        }

        public Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Client results inside OnConnectedAsync Hub methods are not allowed.");
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _proxy.SendCoreAsync(method, args, cancellationToken);
        }
    }
}
