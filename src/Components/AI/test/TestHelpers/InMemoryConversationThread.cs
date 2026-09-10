// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal sealed class InMemoryConversationThread : IConversationThread
{
    private readonly List<ChatResponseUpdate> _updates = [];
    private List<ChatResponseUpdate>? _currentTurn;

    internal InMemoryConversationThread(string threadId)
    {
        ThreadId = threadId;
    }

    public string ThreadId { get; }

    public bool IsStateful { get; private set; }

    public string? ConversationId { get; private set; }

    internal bool HasPendingTurn => _currentTurn is not null;

    public void AppendUserMessage(ChatMessage message)
    {
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
