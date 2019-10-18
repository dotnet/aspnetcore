// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Options used to configure the specified hub type instances. These options override globally set options.
    /// </summary>
    /// <typeparam name="THub">The hub type to configure.</typeparam>
    public class HubOptions<THub> : HubOptions where THub : Hub
    {
        /// <summary>
        /// Add protocols specific to this Hub so other Hubs do not get these protocols by default.
        /// When using this you do not need to add the IHubProtocol to DI.
        /// </summary>
        public IList<IHubProtocol> HubProtocols { get; } = new List<IHubProtocol>();
    }
}
