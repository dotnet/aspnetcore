// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    new ISingleClientProxy Single(string connectionId) => throw new NotImplementedException();
}
