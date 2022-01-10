// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A manager abstraction for adding and removing connections from groups.
/// </summary>
public interface IGroupManager
{
    /// <summary>
    /// Adds a connection to the specified group.
    /// </summary>
    /// <param name="connectionId">The connection ID to add to a group.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous add.</returns>
    Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a connection from the specified group.
    /// </summary>
    /// <param name="connectionId">The connection ID to remove from a group.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous remove.</returns>
    Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
}
