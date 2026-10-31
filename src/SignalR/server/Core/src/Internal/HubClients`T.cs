// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Internal;

[RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
internal sealed class HubClients<THub, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : IHubClients<T> where THub : Hub
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;

    public HubClients(HubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager;
        All = TypedClientBuilder<T>.Build(new AllClientProxy<THub>(_lifetimeManager));
    }

    public T All { get; }

    public T AllExcept(IReadOnlyList<string> excludedConnectionIds)
    {
        return TypedClientBuilder<T>.Build(new AllClientsExceptProxy<THub>(_lifetimeManager, excludedConnectionIds));
    }

    public T Client(string connectionId)
    {
        return TypedClientBuilder<T>.Build(new SingleClientProxy<THub>(_lifetimeManager, connectionId));
    }

    public T Clients(IReadOnlyList<string> connectionIds)
    {
        return TypedClientBuilder<T>.Build(new MultipleClientProxy<THub>(_lifetimeManager, connectionIds));
    }

    public T Group(string groupName)
    {
        return TypedClientBuilder<T>.Build(new GroupProxy<THub>(_lifetimeManager, groupName));
    }

    public T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        return TypedClientBuilder<T>.Build(new GroupExceptProxy<THub>(_lifetimeManager, groupName, excludedConnectionIds));
    }

    public T Groups(IReadOnlyList<string> groupNames)
    {
        return TypedClientBuilder<T>.Build(new MultipleGroupProxy<THub>(_lifetimeManager, groupNames));
    }

    public T User(string userId)
    {
        return TypedClientBuilder<T>.Build(new UserProxy<THub>(_lifetimeManager, userId));
    }

    public T Users(IReadOnlyList<string> userIds)
    {
        return TypedClientBuilder<T>.Build(new MultipleUserProxy<THub>(_lifetimeManager, userIds));
    }
}
