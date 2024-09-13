// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A class that provides <c>dynamic</c> access to connections, including the one that sent the current invocation.
/// </summary>
[RequiresDynamicCodeAttribute("DynamicHubClients requires dynamic code generation to construct a call site.")]
public class DynamicHubClients
{
    private readonly IHubCallerClients _clients;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicHubClients"/> class.
    /// </summary>
    /// <param name="clients">A wrapped <see cref="IHubCallerClients"/> that is used to invoke methods.</param>
    public DynamicHubClients(IHubCallerClients clients)
    {
        _clients = clients;
    }

    /// <summary>
    /// Gets an object that can be used to invoke methods on all clients connected to the hub.
    /// </summary>
    /// <returns>An object that can be used to invoke methods on the specified user.</returns>
    public dynamic All => new DynamicClientProxy(_clients.All);

    /// <summary>
    /// Gets an object that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
    /// <returns>An object that can be used to invoke methods on the specified user.</returns>
    public dynamic AllExcept(IReadOnlyList<string> excludedConnectionIds) => new DynamicClientProxy(_clients.AllExcept(excludedConnectionIds));

    /// <summary>
    /// Gets an object that can be used to invoke methods on the connection which triggered the current invocation.
    /// </summary>
    public dynamic Caller => new DynamicClientProxy(_clients.Caller);

    /// <summary>
    /// Gets an object that can be used to invoke methods on the specified connection.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic Client(string connectionId) => new DynamicClientProxy(_clients.Client(connectionId));

    /// <summary>
    /// Gets an object that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="connectionIds">The connection IDs.</param>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic Clients(IReadOnlyList<string> connectionIds) => new DynamicClientProxy(_clients.Clients(connectionIds));

    /// <summary>
    /// Gets an object that can be used to invoke methods on all connections in the specified group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic Group(string groupName) => new DynamicClientProxy(_clients.Group(groupName));

    /// <summary>
    /// Gets an object that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="groupNames">The group names.</param>
    /// <returns>An object that can be used to invoke methods on the specified user.</returns>
    public dynamic Groups(IReadOnlyList<string> groupNames) => new DynamicClientProxy(_clients.Groups(groupNames));

    /// <summary>
    /// Gets an object that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new DynamicClientProxy(_clients.GroupExcept(groupName, excludedConnectionIds));

    /// <summary>
    /// Gets an object that can be used to invoke methods on connections in a group other than the caller.
    /// </summary>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic OthersInGroup(string groupName) => new DynamicClientProxy(_clients.OthersInGroup(groupName));

    /// <summary>
    /// Gets an object that can be used to invoke methods on connections other than the caller.
    /// </summary>
    public dynamic Others => new DynamicClientProxy(_clients.Others);

    /// <summary>
    /// Gets an object that can be used to invoke methods on all connections associated with the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic User(string userId) => new DynamicClientProxy(_clients.User(userId));

    /// <summary>
    /// Gets an object that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="userIds">The user IDs.</param>
    /// <returns>An object that can be used to invoke methods.</returns>
    public dynamic Users(IReadOnlyList<string> userIds) => new DynamicClientProxy(_clients.Users(userIds));
}
