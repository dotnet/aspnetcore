// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a persistent conversation thread that stores ChatResponseUpdates.
/// Implementations control where and how updates are persisted.
/// </summary>
public interface IConversationThread
{
    /// <summary>
    /// The unique identifier for this thread.
    /// </summary>
    string ThreadId { get; }

    /// <summary>
    /// Whether the remote LLM is stateful (manages its own history).
    /// When true, the LLM returned a ConversationId, so the full message
    /// history does not need to be sent on each turn.
    /// </summary>
    bool IsStateful { get; }

    /// <summary>
    /// The ConversationId returned by a stateful LLM, if any.
    /// This is set on the ChatOptions.ConversationId for stateful LLMs.
    /// </summary>
    string? ConversationId { get; }

    /// <summary>
    /// Appends a user-initiated message as a ChatResponseUpdate to the thread.
    /// Called before the LLM request to record what the user sent.
    /// </summary>
    void AppendUserMessage(ChatMessage message);

    /// <summary>
    /// Appends a single ChatResponseUpdate received during streaming.
    /// Called as each update arrives from the LLM.
    /// </summary>
    void AppendUpdate(ChatResponseUpdate update);

    /// <summary>
    /// Commits the current turn to the stored history.
    /// Only committed turns appear in <see cref="GetUpdates"/> and <see cref="GetMessageHistory"/>.
    /// If a turn fails mid-stream, not calling CompleteTurn discards the partial turn.
    /// </summary>
    void CompleteTurn();

    /// <summary>
    /// Returns all committed updates as a flat list.
    /// Turn boundaries can be detected by looking for updates with
    /// a user Role (from <see cref="AppendUserMessage"/>) or by changes in ResponseId.
    /// </summary>
    IReadOnlyList<ChatResponseUpdate> GetUpdates();

    /// <summary>
    /// Returns the ChatMessage history suitable for sending to a stateless LLM.
    /// This reconstructs ChatMessages from the stored updates.
    /// </summary>
    IReadOnlyList<ChatMessage> GetMessageHistory();
}
