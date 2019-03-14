// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// Describes the current state of the <see cref="HubConnection"/> to the server.
    /// </summary>
    public enum HubConnectionState
    {
        /// <summary>
        /// The hub connection is disconnected.
        /// </summary>
        Disconnected,
        /// <summary>
        /// The hub connection is connected.
        /// </summary>
        Connected
    }
}