// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A clients caller abstraction for a hub.
    /// </summary>
    [Obsolete("The IHubClients<T> interface is obsolete. Use the HubClientBase<T> abstract class instead.", false)]
    public interface IHubCallerClients : IHubCallerClients<IClientProxy> { }
}
