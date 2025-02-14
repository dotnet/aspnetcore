// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Represents the ID being acknowledged so older messages do not need to be buffered anymore.
/// </summary>
public sealed class AckMessage : HubMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AckMessage"/> class.
    /// </summary>
    /// <param name="sequenceId">The ID of the last message that was received.</param>
    public AckMessage(long sequenceId)
    {
        SequenceId = sequenceId;
    }

    /// <summary>
    /// The ID of the last message that was received.
    /// </summary>
    public long SequenceId { get; set; }
}
