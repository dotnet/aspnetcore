// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class ConversationThreadTests
{
    [Fact]
    public void AppendUserMessage_CompleteTurn_StoresUpdate()
    {
        var thread = new InMemoryConversationThread("t1");

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Hello"));
        thread.CompleteTurn();

        var updates = thread.GetUpdates();
        Assert.Single(updates);
        Assert.Equal(ChatRole.User, updates[0].Role);
        var text = Assert.IsType<TextContent>(updates[0].Contents[0]);
        Assert.Equal("Hello", text.Text);
    }

    [Fact]
    public void AppendUpdate_WithCompleteTurn_StoresAssistantUpdates()
    {
        var thread = new InMemoryConversationThread("t1");

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Hi"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("Hello")]
        });
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent(" there")]
        });
        thread.CompleteTurn();

        var updates = thread.GetUpdates();
        Assert.Equal(3, updates.Count); // 1 user + 2 assistant
    }

    [Fact]
    public void MultiTurn_TracksAllTurns()
    {
        var thread = new InMemoryConversationThread("t1");

        // Turn 1
        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "First"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("Reply 1")]
        });
        thread.CompleteTurn();

        // Turn 2
        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Second"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("Reply 2")]
        });
        thread.CompleteTurn();

        // Turn 3
        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Third"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("Reply 3")]
        });
        thread.CompleteTurn();

        var updates = thread.GetUpdates();
        Assert.Equal(6, updates.Count); // 3 user + 3 assistant

        // Verify turn boundaries by counting user-role updates
        var userUpdates = updates.Where(u => u.Role == ChatRole.User).ToList();
        Assert.Equal(3, userUpdates.Count);
    }

    [Fact]
    public void IncompleteTurn_NotInHistory()
    {
        var thread = new InMemoryConversationThread("t1");

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Hello"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("partial")]
        });
        // No CompleteTurn call — simulates a failed turn

        Assert.Empty(thread.GetUpdates());
    }

    [Fact]
    public void GetMessageHistory_ReconstructsMessages()
    {
        var thread = new InMemoryConversationThread("t1");

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "What is 2+2?"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("4")]
        });
        thread.CompleteTurn();

        var history = thread.GetMessageHistory();
        Assert.Equal(2, history.Count);

        Assert.Equal(ChatRole.User, history[0].Role);
        var userText = Assert.IsType<TextContent>(history[0].Contents[0]);
        Assert.Equal("What is 2+2?", userText.Text);

        Assert.Equal(ChatRole.Assistant, history[1].Role);
    }

    [Fact]
    public void DetectsStatefulLLM_WhenConversationIdPresent()
    {
        var thread = new InMemoryConversationThread("t1");

        Assert.False(thread.IsStateful);
        Assert.Null(thread.ConversationId);

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Hi"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            ConversationId = "conv-abc",
            Contents = [new TextContent("Hello")]
        });
        thread.CompleteTurn();

        Assert.True(thread.IsStateful);
        Assert.Equal("conv-abc", thread.ConversationId);
    }

    [Fact]
    public void RemainsStateless_WhenNoConversationId()
    {
        var thread = new InMemoryConversationThread("t1");

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Hi"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("Hello")]
        });
        thread.CompleteTurn();

        Assert.False(thread.IsStateful);
        Assert.Null(thread.ConversationId);
    }

    [Fact]
    public void ThreadId_IsPreserved()
    {
        var thread = new InMemoryConversationThread("my-thread-123");
        Assert.Equal("my-thread-123", thread.ThreadId);
    }

    [Fact]
    public void CompleteTurn_WithoutAppend_DoesNothing()
    {
        var thread = new InMemoryConversationThread("t1");
        thread.CompleteTurn(); // No AppendUserMessage was called

        Assert.Empty(thread.GetUpdates());
    }

    [Fact]
    public void IncompleteTurn_ThenNewTurn_DiscardsIncomplete()
    {
        var thread = new InMemoryConversationThread("t1");

        // Incomplete turn
        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "First"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("partial")]
        });
        // No CompleteTurn — discard

        // New complete turn
        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Second"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("complete")]
        });
        thread.CompleteTurn();

        var updates = thread.GetUpdates();
        Assert.Equal(2, updates.Count);

        var userText = Assert.IsType<TextContent>(updates[0].Contents[0]);
        Assert.Equal("Second", userText.Text);
    }

    [Fact]
    public void GetMessageHistory_MultiTurn_ReconstructsCorrectly()
    {
        var thread = new InMemoryConversationThread("t1");

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Q1"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-1",
            Contents = [new TextContent("A1")]
        });
        thread.CompleteTurn();

        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Q2"));
        thread.AppendUpdate(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "msg-2",
            Contents = [new TextContent("A2")]
        });
        thread.CompleteTurn();

        var history = thread.GetMessageHistory();
        Assert.Equal(4, history.Count);

        Assert.Equal(ChatRole.User, history[0].Role);
        Assert.Equal(ChatRole.Assistant, history[1].Role);
        Assert.Equal(ChatRole.User, history[2].Role);
        Assert.Equal(ChatRole.Assistant, history[3].Role);
    }
}
