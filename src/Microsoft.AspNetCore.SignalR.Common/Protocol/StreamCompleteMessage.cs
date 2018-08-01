// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// A message for indicating that a particular stream has ended.
    /// </summary>
    public class StreamCompleteMessage : HubMessage
    {
        /// <summary>
        /// Gets the stream id.
        /// </summary>
        public string StreamId { get; }

        /// <summary>
        /// Gets the error. Will be null if there is no error.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Whether the message has an error.
        /// </summary>
        public bool HasError { get => Error != null; }

        /// <summary>
        /// Initializes a new instance of <see cref="StreamCompleteMessage"/>
        /// </summary>
        /// <param name="streamId">The streamId of the stream to complete.</param>
        /// <param name="error">An optional error field.</param>
        public StreamCompleteMessage(string streamId, string error = null) 
        {
            StreamId = streamId;
            Error = error;
        }
    }
}
