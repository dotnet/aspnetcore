// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// A handshake response message.
    /// </summary>
    public class HandshakeResponseMessage : HubMessage
    {
        /// <summary>
        /// An empty response message with no error.
        /// </summary>
        public static readonly HandshakeResponseMessage Empty = new HandshakeResponseMessage(null);

        /// <summary>
        /// Gets the optional error message.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResponseMessage"/> class.
        /// </summary>
        /// <param name="error">An optional response error message. A <c>null</c> error message indicates a succesful handshake.</param>
        public HandshakeResponseMessage(string error)
        {
            // Note that a response with an empty string for error in the JSON is considered an errored response
            Error = error;
        }
    }
}
