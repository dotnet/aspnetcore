// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal class HubConnectionBinder<THub> : IInvocationBinder where THub : Hub
{
    private readonly HubDispatcher<THub> _dispatcher;
    private readonly HubConnectionContext _connection;
    private readonly HubLifetimeManager<THub> _hubLifetimeManager;

    public HubConnectionBinder(HubDispatcher<THub> dispatcher, HubLifetimeManager<THub> lifetimeManager, HubConnectionContext connection)
    {
        _dispatcher = dispatcher;
        _connection = connection;
        _hubLifetimeManager = lifetimeManager;
    }

    public IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        return _dispatcher.GetParameterTypes(methodName);
    }

    public Type GetReturnType(string invocationId)
    {
        if (_hubLifetimeManager.TryGetReturnType(invocationId, out var type))
        {
            return type;
        }
        throw new InvalidOperationException("Unknown invocation ID.");
    }

    public Type GetStreamItemType(string streamId)
    {
        return _connection.StreamTracker.GetStreamItemType(streamId);
    }
}
