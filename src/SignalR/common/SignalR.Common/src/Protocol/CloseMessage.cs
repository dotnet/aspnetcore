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
        /// An empty close message with no error.
        /// </summary>
        public static readonly CloseMessage Empty = new CloseMessage(null);

        /// <summary>
        /// Gets the optional error message.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseMessage"/> class with an optional error message.
        /// </summary>
        /// <param name="error">An optional error message.</param>
        public CloseMessage(string error)
        {
            Error = error;
        }
    }
}
