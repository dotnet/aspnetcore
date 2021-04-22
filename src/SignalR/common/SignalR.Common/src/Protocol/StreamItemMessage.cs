// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Represents a single item of an active stream.
    /// </summary>
    public class StreamItemMessage : HubInvocationMessage
    {
        /// <summary>
        /// The single item from a stream.
        /// </summary>
        public object? Item { get; set; }

        /// <summary>
        /// Constructs a <see cref="StreamItemMessage"/>.
        /// </summary>
        /// <param name="invocationId">The ID of the stream.</param>
        /// <param name="item">An item from the stream.</param>
        public StreamItemMessage(string invocationId, object? item) : base(invocationId)
        {
            Item = item;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"StreamItem {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Item)}: {Item ?? "<<null>>"} }}";
        }
    }
}
