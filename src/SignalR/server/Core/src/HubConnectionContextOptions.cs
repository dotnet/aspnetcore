// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Options used to configure <see cref="HubConnectionContext"/>.
    /// </summary>
    public class HubConnectionContextOptions
    {
        /// <summary>
        /// Gets or sets the interval used to send keep alive pings to connected clients.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; }

        /// <summary>
        /// Gets or sets the time window clients have to send a message before the server closes the connection.
        /// </summary>
        public TimeSpan ClientTimeoutInterval { get; set; }

        /// <summary>
        /// Gets or sets the max buffer size for client upload streams.
        /// </summary>
        public int StreamBufferCapacity { get; set; }

        /// <summary>
        /// Gets or sets the maximum message size the client can send.
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; }
    }
}
