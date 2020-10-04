// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// A keep-alive message to let the other side of the connection know that the connection is still alive.
    /// </summary>
    public class PingMessage : HubMessage
    {
        /// <summary>
        /// A static instance of the PingMessage to remove unneeded allocations.
        /// </summary>
        public static readonly PingMessage Instance = new PingMessage();

        private PingMessage()
        {
        }
    }
}
