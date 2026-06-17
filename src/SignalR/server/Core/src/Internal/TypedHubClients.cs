// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR.Internal;

[RequiresDynamicCode("Creating a proxy instance requires generating code at runtime")]
internal sealed class TypedHubClients<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : IHubCallerClients<T>
{
    private readonly IHubCallerClients _hubClients;

    public TypedHubClients(IHubCallerClients dynamicContext)
    {
        _hubClients = dynamicContext;
    }

    public T Client(string connectionId) => TypedClientBuilder<T>.Build(_hubClients.Client(connectionId));

    public T All => TypedClientBuilder<T>.Build(_hubClients.All);

    public T Caller => TypedClientBuilder<T>.Build(_hubClients.Caller);

    public T Others => TypedClientBuilder<T>.Build(_hubClients.Others);

    public T AllExcept(IReadOnlyList<string> excludedConnectionIds) => TypedClientBuilder<T>.Build(_hubClients.AllExcept(excludedConnectionIds));

    public T Group(string groupName)
    {
        return TypedClientBuilder<T>.Build(_hubClients.Group(groupName));
    }

    public T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        return TypedClientBuilder<T>.Build(_hubClients.GroupExcept(groupName, excludedConnectionIds));
    }

    public T Clients(IReadOnlyList<string> connectionIds)
    {
        return TypedClientBuilder<T>.Build(_hubClients.Clients(connectionIds));
    }

    public T Groups(IReadOnlyList<string> groupNames)
    {
        return TypedClientBuilder<T>.Build(_hubClients.Groups(groupNames));
    }

    public T OthersInGroup(string groupName)
    {
        return TypedClientBuilder<T>.Build(_hubClients.OthersInGroup(groupName));
    }

    public T User(string userId)
    {
        return TypedClientBuilder<T>.Build(_hubClients.User(userId));
    }

    public T Users(IReadOnlyList<string> userIds)
    {
        return TypedClientBuilder<T>.Build(_hubClients.Users(userIds));
    }
}
