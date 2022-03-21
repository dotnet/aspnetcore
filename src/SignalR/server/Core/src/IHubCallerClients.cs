// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A clients caller abstraction for a hub.
/// </summary>
public interface IHubCallerClients : IHubCallerClients<IClientProxy>
{
    /// <summary>
    /// Gets a proxy that can be used to invoke methods on a single client connected to the hub and receive results.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <returns>A client caller.</returns>
    new ISingleClientProxy Single(string connectionId) => throw new NotImplementedException();
}
