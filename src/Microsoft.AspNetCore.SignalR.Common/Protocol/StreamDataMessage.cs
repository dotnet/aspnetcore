// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Sent to parameter streams. 
    /// Similar to <see cref="StreamItemMessage"/>, except the data is sent to a parameter stream, rather than in response to an invocation.
    /// </summary>
    public class StreamDataMessage : HubMessage
    {
        /// <summary>
        /// The piece of data this message carries.
        /// </summary>
        public object Item { get; }

        /// <summary>
        /// The stream to which to deliver data.
        /// </summary>
        public string StreamId { get; }

        public StreamDataMessage(string streamId, object item)
        {
            StreamId = streamId;
            Item = item;
        }

        public override string ToString()
        {
            return $"StreamDataMessage {{ {nameof(StreamId)}: \"{StreamId}\", {nameof(Item)}: {Item ?? "<<null>>"} }}";
        }
    }
}
