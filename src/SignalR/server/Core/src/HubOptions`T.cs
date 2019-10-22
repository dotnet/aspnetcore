// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Options used to configure the specified hub type instances. These options override globally set options.
    /// </summary>
    /// <typeparam name="THub">The hub type to configure.</typeparam>
    public class HubOptions<THub> : HubOptions where THub : Hub
    {
        internal bool UserHasSetValues { get; set; }
    }
}
