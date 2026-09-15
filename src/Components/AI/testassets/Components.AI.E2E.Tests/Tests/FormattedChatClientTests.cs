// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using DojoClient.Formatting;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class FormattedChatClientTests
{
    [TestMethod]
    public async Task TextUpdatesWithSameMessageId_AreCoalesced()
    {
        using var client = CreateClient(
            CreateTextUpdate("message-1", "Hello"),
            CreateTextUpdate("message-1", " world"));

        var updates = await CollectAsync(client);

        Assert.AreEqual("Hello", GetRichText(updates[0]).Text);
        Assert.AreEqual("Hello world", GetRichText(updates[1]).Text);
    }

    [TestMethod]
    public async Task TextUpdatesWithDifferentMessageIds_AreIndependent()
    {
        using var client = CreateClient(
            CreateTextUpdate("message-1", "First"),
            CreateTextUpdate("message-2", "Second"),
            CreateTextUpdate("message-1", " continued"));

        var updates = await CollectAsync(client);

        Assert.AreEqual("message-1", updates[0].MessageId);
        Assert.AreEqual("message-2", updates[1].MessageId);
        Assert.AreEqual("message-1", updates[2].MessageId);
        Assert.AreEqual("First", GetRichText(updates[0]).Text);
        Assert.AreEqual("Second", GetRichText(updates[1]).Text);
        Assert.AreEqual("First continued", GetRichText(updates[2]).Text);
    }

    [TestMethod]
    public async Task TextUpdatesWithoutMessageId_AreNotCoalesced()
    {
        var text = new TextContent("Unidentified");
        using var client = CreateClient(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [text],
        });

        var updates = await CollectAsync(client);

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual(1, updates[0].Contents.Count);
        Assert.AreSame(text, updates[0].Contents[0]);
    }

    [TestMethod]
    public async Task NonTextUpdates_ArePassedThrough()
    {
        var content = new AIContent();
        using var client = CreateClient(new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "message-1",
            Contents = [content],
        });

        var updates = await CollectAsync(client);

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual(1, updates[0].Contents.Count);
        Assert.AreSame(content, updates[0].Contents[0]);
    }

    private static FormattedChatClient CreateClient(params ChatResponseUpdate[] updates)
        => new(new TestChatClient(updates));

    private static ChatResponseUpdate CreateTextUpdate(string messageId, string text) => new()
    {
        Role = ChatRole.Assistant,
        MessageId = messageId,
        Contents = [new TextContent(text)],
    };

    private static RichTextContent GetRichText(ChatResponseUpdate update)
    {
        return update.Contents.OfType<RichTextContent>().Single();
    }

    private static async Task<List<ChatResponseUpdate>> CollectAsync(IChatClient client)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([]))
        {
            updates.Add(update);
        }

        return updates;
    }

    private sealed class TestChatClient(IReadOnlyList<ChatResponseUpdate> updates) : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
                await Task.Yield();
            }
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => GetStreamingResponseAsync(messages, options, cancellationToken)
                .ToChatResponseAsync(cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient) ? this : null;

        public void Dispose()
        {
        }
    }

}
