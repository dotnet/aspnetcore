// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A <see cref="Hub"/> activator abstraction.
/// </summary>
/// <typeparam name="THub">The hub type.</typeparam>
public interface IHubActivator<[DynamicallyAccessedMembers(Hub.DynamicallyAccessedMembers)] THub> where THub : Hub
{
    /// <summary>
    /// Creates a hub.
    /// </summary>
    /// <returns>The created hub.</returns>
    THub Create();

    /// <summary>
    /// Releases the specified hub.
    /// </summary>
    /// <param name="hub">The hub to release.</param>
    void Release(THub hub);
}
