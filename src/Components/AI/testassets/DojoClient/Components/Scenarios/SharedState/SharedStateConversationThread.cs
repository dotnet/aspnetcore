// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace DojoClient.Components.Scenarios.SharedState;

internal sealed class SharedStateConversationThread : IConversationThread
{
    private readonly List<ChatResponseUpdate> _updates = [];
    private List<ChatResponseUpdate>? _currentTurn;

    internal SharedStateConversationThread(string threadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(threadId);
        ThreadId = threadId;
    }

    public string ThreadId { get; }

    public bool IsStateful { get; private set; }

    public string? ConversationId { get; private set; }

    public void AppendUserMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _currentTurn =
        [
            new ChatResponseUpdate
            {
                Role = message.Role,
                Contents = [.. message.Contents],
            },
        ];
    }

    public void AppendUpdate(ChatResponseUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        _currentTurn?.Add(update);

        if (update.ConversationId is not null)
        {
            IsStateful = true;
            ConversationId = update.ConversationId;
        }
    }

    public void CompleteTurn()
    {
        if (_currentTurn is not null)
        {
            _updates.AddRange(_currentTurn);
            _currentTurn = null;
        }
    }

    public IReadOnlyList<ChatResponseUpdate> GetUpdates() => _updates;
}
