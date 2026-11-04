// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Represents the restart of the sequence of messages being sent. <see cref="SequenceId"/> is the starting ID of messages being sent, which might be duplicate messages.
/// </summary>
public sealed class SequenceMessage : HubMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceMessage"/> class.
    /// </summary>
    /// <param name="sequenceId">Specifies the starting ID for messages that will be received from this point onward.</param>
    public SequenceMessage(long sequenceId)
    {
        SequenceId = sequenceId;
    }

    /// <summary>
    /// The new starting ID of incoming messages.
    /// </summary>
    public long SequenceId { get; set; }
}
