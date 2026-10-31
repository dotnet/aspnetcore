// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// An abstraction that provides access to client connections.
/// </summary>
public interface IHubClients : IHubClients<IClientProxy>
{
    /// <summary>
    /// Gets a proxy that can be used to invoke methods on a single client connected to the hub and receive results.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <returns>A client caller.</returns>
    new ISingleClientProxy Client(string connectionId) => new NonInvokingSingleClientProxy(((IHubClients<IClientProxy>)this).Client(connectionId), "IHubClients.Client(string connectionId)");
}
