// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A context abstraction for a hub.
/// </summary>
public interface IHubContext
{
    /// <summary>
    /// Gets a <see cref="IHubClients"/> that can be used to invoke methods on clients connected to the hub.
    /// </summary>
    IHubClients Clients { get; }

    /// <summary>
    /// Gets a <see cref="IGroupManager"/> that can be used to add and remove connections to named groups.
    /// </summary>
    IGroupManager Groups { get; }
}

/// <summary>
/// A context abstraction for a hub.
/// </summary>
public interface IHubContext<out THub> where THub : Hub
{
    /// <summary>
    /// Gets a <see cref="IHubClients"/> that can be used to invoke methods on clients connected to the hub.
    /// </summary>
    IHubClients Clients { get; }

    /// <summary>
    /// Gets a <see cref="IGroupManager"/> that can be used to add and remove connections to named groups.
    /// </summary>
    IGroupManager Groups { get; }
}
