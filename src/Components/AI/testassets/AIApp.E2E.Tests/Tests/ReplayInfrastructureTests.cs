// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using AIApp.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[TestClass]
public class ReplayInfrastructureTests
{
    [TestMethod]
    public async Task CapturingChatClient_RecordsDecodedMessagesAndUpdates()
    {
        var fake = new FakeChatClient();
        fake.Enqueue((messages, _, cancellationToken) =>
        {
            Assert.AreEqual("hello", messages.Single().Text);
            return YieldAsync(
                [new ChatResponseUpdate(ChatRole.Assistant, "captured")],
                cancellationToken);
        });
        using var client = new CapturingChatClient(fake);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]);

        Assert.AreEqual("captured", response.Text);
        Assert.HasCount(1, client.Calls);
        Assert.AreEqual("hello", client.Calls[0].Messages.Single().Text);
        Assert.AreEqual("captured", client.Calls[0].Updates.Single().Text);
    }

    [TestMethod]
    public async Task GatedReplayChatClient_WaitsAfterNamedFrame()
    {
        var script = new ReplayCheckpointScript
        {
            Calls =
            [
                new ReplayCall
                {
                    Request = new ReplayRequestExpectation
                    {
                        LastUserMessage = "hello",
                        MessageCount = 1,
                    },
                    Frames =
                    [
                        new ReplayFrame
                        {
                            Name = "assistant-text",
                            Updates = [new ChatResponseUpdate(ChatRole.Assistant, "hello back")],
                        },
                    ],
                },
            ],
        };
        var locks = new TestLockProvider();
        var session = new TestSessionContext { Id = "session" };
        using var client = new GatedReplayChatClient(script, locks, session);
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual("hello back", enumerator.Current.Text);

        var completion = enumerator.MoveNextAsync().AsTask();
        Assert.IsFalse(completion.IsCompleted);

        locks.Release($"session:{script.GetLockName(0, 0)}");

        Assert.IsFalse(await completion);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldAsync(
        IEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }

        await Task.CompletedTask;
    }
}
