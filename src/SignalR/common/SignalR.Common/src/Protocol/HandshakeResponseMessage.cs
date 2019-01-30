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
        public static readonly HandshakeResponseMessage Empty = new HandshakeResponseMessage(error: null);

        /// <summary>
        /// Gets the optional error message.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Highest minor protocol version that the server supports.
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResponseMessage"/> class.
        /// An error response does need a minor version. Since the handshake has failed, any extra data will be ignored.
        /// </summary>
        /// <param name="error">Error encountered by the server, indicating why the handshake has failed.</param>
        public HandshakeResponseMessage(string error) : this(null, error) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResponseMessage"/> class.
        /// A reponse with a minor version indicates success, and doesn't require an error field.
        /// </summary>
        /// <param name="minorVersion">The highest protocol minor version that the server supports.</param>
        public HandshakeResponseMessage(int minorVersion) : this(minorVersion, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandshakeResponseMessage"/> class.
        /// </summary>
        /// <param name="error">Error encountered by the server, indicating why the handshake has failed.</param>
        /// <param name="minorVersion">The highest protocol minor version that the server supports.</param>
        public HandshakeResponseMessage(int? minorVersion, string error)
        {
            // MinorVersion defaults to 0, because old servers don't send a minor version 
            MinorVersion = minorVersion ?? 0;
            Error = error;
        }
    }
}
