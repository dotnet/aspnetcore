// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal class HubConnectionBinder<THub> : IInvocationBinder where THub : Hub
{
    private readonly HubDispatcher<THub> _dispatcher;
    private readonly HubConnectionContext _connection;

    public HubConnectionBinder(HubDispatcher<THub> dispatcher, HubConnectionContext connection)
    {
        _dispatcher = dispatcher;
        _connection = connection;
    }

    public IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        return _dispatcher.GetParameterTypes(methodName);
    }

    public Type GetReturnType(string invocationId)
    {
        return typeof(object);
    }

    public Type GetStreamItemType(string streamId)
    {
        return _connection.StreamTracker.GetStreamItemType(streamId);
    }
}
