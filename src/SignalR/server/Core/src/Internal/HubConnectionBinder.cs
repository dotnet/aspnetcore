// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubConnectionBinder<[DynamicallyAccessedMembers(Hub.DynamicallyAccessedMembers)] THub> : IInvocationBinder where THub : Hub
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
        // If the id isn't found then it's possible the server canceled the request for a result but the client still sent the result.
        throw new InvalidOperationException($"Unknown invocation ID '{invocationId}'.");
    }

    public Type GetStreamItemType(string streamId)
    {
        return _connection.StreamTracker.GetStreamItemType(streamId);
    }

    public string? GetTarget(ReadOnlySpan<byte> targetUtf8Bytes)
    {
        return _dispatcher.GetTargetName(targetUtf8Bytes);
    }
}
