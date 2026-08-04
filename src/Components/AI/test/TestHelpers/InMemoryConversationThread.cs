// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal sealed class InMemoryConversationThread : IConversationThread
{
    private readonly List<ChatResponseUpdate> _updates = new();
    private List<ChatResponseUpdate>? _currentTurn;

    internal InMemoryConversationThread(string threadId)
    {
        ThreadId = threadId;
    }

    public string ThreadId { get; }
    public bool IsStateful { get; private set; }
    public string? ConversationId { get; private set; }

    public void AppendUserMessage(ChatMessage message)
    {
        _currentTurn = new List<ChatResponseUpdate>();

        _currentTurn.Add(new ChatResponseUpdate
        {
            Role = message.Role,
            Contents = [.. message.Contents]
        });
    }

    public void AppendUpdate(ChatResponseUpdate update)
    {
        _currentTurn?.Add(update);

        if (!IsStateful && update.ConversationId is not null)
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

    public IReadOnlyList<ChatMessage> GetMessageHistory()
    {
        var messages = new List<ChatMessage>();
        List<ChatResponseUpdate>? currentGroup = null;

        foreach (var update in _updates)
        {
            // A user-role update marks the start of a new turn
            if (update.Role == ChatRole.User)
            {
                // Flush previous assistant group
                if (currentGroup is { Count: > 0 })
                {
                    var response = currentGroup.ToChatResponse();
                    foreach (var msg in response.Messages)
                    {
                        messages.Add(msg);
                    }
                }

                messages.Add(new ChatMessage(ChatRole.User, [.. update.Contents]));
                currentGroup = new List<ChatResponseUpdate>();
            }
            else
            {
                currentGroup ??= new List<ChatResponseUpdate>();
                currentGroup.Add(update);
            }
        }

        // Flush trailing assistant group
        if (currentGroup is { Count: > 0 })
        {
            var response = currentGroup.ToChatResponse();
            foreach (var msg in response.Messages)
            {
                messages.Add(msg);
            }
        }

        return messages;
    }
}
