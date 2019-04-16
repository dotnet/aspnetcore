// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// An abstraction that provides access to client connections.
    /// </summary>
    [Obsolete("The IHubClients interface is obsolete. Use the HubClientBase abstract class instead.", false)]
    public interface IHubClients : IHubClients<IClientProxy> { }
}
