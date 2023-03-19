// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubCallerClients : IHubCallerClients
{
    private readonly string _connectionId;
    private readonly IHubClients _hubClients;
    internal readonly ChannelBasedSemaphore _parallelInvokes;

    private int _shouldReleaseSemaphore = 1;

    // Client results don't work in OnConnectedAsync
    // This property is set by the hub dispatcher when those methods are being called
    // so we can prevent users from making blocking client calls by returning a custom ISingleClientProxy instance
    internal bool InvokeAllowed { get; set; }

    public HubCallerClients(IHubClients hubClients, string connectionId, ChannelBasedSemaphore parallelInvokes)
    {
        _connectionId = connectionId;
        _hubClients = hubClients;
        _parallelInvokes = parallelInvokes;
    }

    IClientProxy IHubCallerClients<IClientProxy>.Caller => Caller;
    public ISingleClientProxy Caller
    {
        get
        {
            if (!InvokeAllowed)
            {
                return new NoInvokeSingleClientProxy(_hubClients.Client(_connectionId));
            }
            return new SingleClientProxy(_hubClients.Client(_connectionId), this);
        }
    }

    public IClientProxy Others => _hubClients.AllExcept(new[] { _connectionId });

    public IClientProxy All => _hubClients.All;

    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
    {
        return _hubClients.AllExcept(excludedConnectionIds);
    }

    IClientProxy IHubClients<IClientProxy>.Client(string connectionId) => Client(connectionId);
    public ISingleClientProxy Client(string connectionId)
    {
        if (!InvokeAllowed)
        {
            return new NoInvokeSingleClientProxy(_hubClients.Client(connectionId));
        }
        return new SingleClientProxy(_hubClients.Client(connectionId), this);
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
        return _hubClients.GroupExcept(groupName, new[] { _connectionId });
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

    // false if semaphore is being released by another caller, true if you own releasing the semaphore
    internal bool TrySetSemaphoreReleased()
    {
        return Interlocked.CompareExchange(ref _shouldReleaseSemaphore, 0, 1) == 1;
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

    private sealed class SingleClientProxy : ISingleClientProxy
    {
        private readonly ISingleClientProxy _proxy;
        private readonly HubCallerClients _hubCallerClients;

        public SingleClientProxy(ISingleClientProxy hubClients, HubCallerClients hubCallerClients)
        {
            _proxy = hubClients;
            _hubCallerClients = hubCallerClients;
        }

        public async Task<T> InvokeCoreAsync<T>(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            // Releases the Channel that is blocking pending invokes, which in turn can block the receive loop.
            // Because we are waiting for a result from the client we need to let the receive loop run otherwise we'll be blocked forever
            var value = _hubCallerClients.TrySetSemaphoreReleased();
            // Only release once, and we set ShouldReleaseSemaphore to 0 so the DefaultHubDispatcher knows not to call Release again
            if (value)
            {
                _hubCallerClients._parallelInvokes.Release();
            }
            var result = await _proxy.InvokeCoreAsync<T>(method, args, cancellationToken);
            return result;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _proxy.SendCoreAsync(method, args, cancellationToken);
        }
    }
}
