// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A resolver abstraction for working with <see cref="IHubProtocol"/> instances.
/// </summary>
public interface IHubProtocolResolver
{
    /// <summary>
    /// Gets a collection of all available hub protocols.
    /// </summary>
    IReadOnlyList<IHubProtocol> AllProtocols { get; }

    /// <summary>
    /// Gets the hub protocol with the specified name, if it is allowed by the specified list of supported protocols.
    /// </summary>
    /// <param name="protocolName">The protocol name.</param>
    /// <param name="supportedProtocols">A collection of supported protocols.</param>
    /// <returns>A matching <see cref="IHubProtocol"/> or <c>null</c> if no matching protocol was found.</returns>
    IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols);
}
