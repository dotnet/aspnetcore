// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Options used to configure hub instances.
    /// </summary>
    public class HubOptions
    {
        // HandshakeTimeout and KeepAliveInterval are set to null here to help identify when
        // local hub options have been set. Global default values are set in HubOptionsSetup.
        // SupportedProtocols being null is the true default value, and it represents support
        // for all available protocols.

        /// <summary>
        /// Gets or sets the interval used by the server to timeout incoming handshake requests by clients. The default timeout is 15 seconds.
        /// </summary>
        public TimeSpan? HandshakeTimeout { get; set; } = null;

        /// <summary>
        /// Gets or sets the interval used by the server to send keep alive pings to connected clients. The default interval is 15 seconds.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; } = null;

        /// <summary>
        /// Gets or sets the time window clients have to send a message before the server closes the connection. The default timeout is 30 seconds.
        /// </summary>
        public TimeSpan? ClientTimeoutInterval { get; set; } = null;

        /// <summary>
        /// Gets or sets a collection of supported hub protocol names.
        /// </summary>
        public IList<string> SupportedProtocols { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum message size of a single incoming hub message. The default is 32KB.
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether detailed error messages are sent to the client.
        /// Detailed error messages include details from exceptions thrown on the server.
        /// </summary>
        public bool? EnableDetailedErrors { get; set; } = null;

        /// <summary>
        /// Gets or sets the max buffer size for client upload streams. The default size is 10.
        /// </summary>
        public int? StreamBufferCapacity { get; set; } = null;
    }
}
