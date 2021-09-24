// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// An abstraction that provides access to client connections.
    /// </summary>
    public interface IHubClients : IHubClients<IClientProxy> { }
}
