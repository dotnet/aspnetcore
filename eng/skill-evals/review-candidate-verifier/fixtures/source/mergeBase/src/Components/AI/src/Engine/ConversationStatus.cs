// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// The state of a conversation driven by an <see cref="AgentContext"/>.
/// </summary>
public enum ConversationStatus
{
    /// <summary>
    /// No response is in flight; the user can send a message.
    /// </summary>
    Idle,

    /// <summary>
    /// A model response is streaming.
    /// </summary>
    Streaming,

    /// <summary>
    /// The conversation is waiting for the user to interact with the current response.
    /// </summary>
    AwaitingInput,

    /// <summary>
    /// The last turn failed. <see cref="AgentContext.Error"/> holds the exception and
    /// <see cref="AgentContext.RetryAsync(CancellationToken)"/> replays the turn.
    /// </summary>
    Error
}
