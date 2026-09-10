// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a persistent conversation thread that stores streamed chat updates.
/// </summary>
/// <remarks>
/// Implementations control where and how conversation updates are persisted.
/// </remarks>
public interface IConversationThread
{
    /// <summary>
    /// Gets the unique identifier for this thread.
    /// </summary>
    string ThreadId { get; }

    /// <summary>
    /// Gets a value indicating whether the remote service manages the conversation history.
    /// </summary>
    bool IsStateful { get; }

    /// <summary>
    /// Gets the conversation identifier returned by a stateful remote service, if any.
    /// </summary>
    string? ConversationId { get; }

    /// <summary>
    /// Begins a turn by appending the message sent to the chat client.
    /// </summary>
    /// <param name="message">The message that begins the turn.</param>
    void AppendUserMessage(ChatMessage message);

    /// <summary>
    /// Appends an update received from the chat client to the current turn.
    /// </summary>
    /// <param name="update">The streamed update.</param>
    void AppendUpdate(ChatResponseUpdate update);

    /// <summary>
    /// Commits the current turn to the stored history.
    /// </summary>
    void CompleteTurn();

    /// <summary>
    /// Gets all committed updates in chronological order.
    /// </summary>
    /// <returns>The committed updates.</returns>
    IReadOnlyList<ChatResponseUpdate> GetUpdates();
}
