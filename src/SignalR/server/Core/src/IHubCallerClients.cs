// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// A clients caller abstraction for a hub.
/// </summary>
public interface IHubCallerClients : IHubCallerClients<IClientProxy>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    new ISingleClientProxy Single(string connectionId) => throw new NotImplementedException();
}
