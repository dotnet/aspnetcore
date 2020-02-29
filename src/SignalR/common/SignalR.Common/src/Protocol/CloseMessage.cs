// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// The message sent when closing a connection.
    /// </summary>
    public class CloseMessage : HubMessage
    {
        /// <summary>
        /// An empty close message with no error and <see cref="AllowReconnect"/> set to <see langword="false"/>.
        /// </summary>
        public static readonly CloseMessage Empty = new CloseMessage(error: null, allowReconnect: false);

        /// <summary>
        /// Gets the optional error message.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// If <see langword="false"/>, clients with automatic reconnects enabled should not attempt to automatically reconnect after receiving the <see cref="CloseMessage"/>.
        /// </summary>
        public bool AllowReconnect { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseMessage"/> class with an optional error message and <see cref="AllowReconnect"/> set to <see langword="false"/>.
        /// </summary>
        /// <param name="error">An optional error message.</param>
        public CloseMessage(string error)
            : this(error, allowReconnect: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseMessage"/> class with an optional error message and a <see cref="bool"/> indicating whether or not a client with
        /// automatic reconnects enabled should attempt to reconnect upon receiving the message.
        /// </summary>
        /// <param name="error">An optional error message.</param>
        /// <param name="allowReconnect">
        /// <see langword="true"/>, if client with automatic reconnects enabled should attempt to reconnect after receiving the <see cref="CloseMessage"/>;
        /// <see langword="false"/>, if the client should not try to reconnect whether or not automatic reconnects are enabled.
        /// </param>
        public CloseMessage(string error, bool allowReconnect)
        {
            Error = error;
            AllowReconnect = allowReconnect;
        }
    }
}
