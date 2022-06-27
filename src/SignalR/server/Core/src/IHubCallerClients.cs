// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Internal;

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
    new ISingleClientProxy Client(string connectionId) => new NonInvokingSingleClientProxy(((IHubCallerClients<IClientProxy>)this).Client(connectionId), "IHubCallerClients.Client(string connectionId)");

    /// <summary>
    /// Gets a proxy that can be used to invoke methods on the calling client and receive results.
    /// </summary>
    /// <returns>A client caller.</returns>
    new ISingleClientProxy Caller => new NonInvokingSingleClientProxy(((IHubCallerClients<IClientProxy>)this).Caller, "IHubCallerClients.Caller");
}
