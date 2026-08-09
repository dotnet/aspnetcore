// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using AIApp.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.AI;
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

    [TestMethod]
    public async Task GatedReplayChatClient_CancelsCheckpointWait()
    {
        var script = CreateSingleFrameScript();
        var locks = new TestLockProvider();
        var session = new TestSessionContext { Id = "cancel-session" };
        using var client = new GatedReplayChatClient(script, locks, session);
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")],
            cancellationToken: cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.IsTrue(await enumerator.MoveNextAsync());
        var completion = enumerator.MoveNextAsync().AsTask();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await completion);
    }

    [TestMethod]
    public async Task GatedReplayChatClient_IsolatesSessionsAndCalls()
    {
        var script = new ReplayCheckpointScript
        {
            Calls = [CreateSingleFrameCall(), CreateSingleFrameCall()],
        };
        var locks = new TestLockProvider();
        using var firstClient = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = "first-session" });
        using var secondClient = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = "second-session" });

        await using var firstCall = firstClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();
        await using var nextCall = firstClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();
        await using var otherSessionCall = secondClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();

        Assert.IsTrue(await firstCall.MoveNextAsync());
        Assert.IsTrue(await nextCall.MoveNextAsync());
        Assert.IsTrue(await otherSessionCall.MoveNextAsync());
        var firstCompletion = firstCall.MoveNextAsync().AsTask();
        var nextCompletion = nextCall.MoveNextAsync().AsTask();
        var otherSessionCompletion = otherSessionCall.MoveNextAsync().AsTask();

        locks.Release($"first-session:{script.GetLockName(0, 0)}");

        Assert.IsFalse(await firstCompletion);
        Assert.IsFalse(nextCompletion.IsCompleted);
        Assert.IsFalse(otherSessionCompletion.IsCompleted);

        locks.Release($"first-session:{script.GetLockName(1, 0)}");
        locks.Release($"second-session:{script.GetLockName(0, 0)}");

        Assert.IsFalse(await nextCompletion);
        Assert.IsFalse(await otherSessionCompletion);
    }

    [TestMethod]
    public async Task AgenticChatScript_DecodesActionAndContinues()
    {
        var script = ReplayCheckpointScript.Load("Dojo_AgenticChat.recording.json");
        var locks = new TestLockProvider();
        const string sessionId = "agentic-chat-unit";
        for (var callIndex = 0; callIndex < script.Calls.Count; callIndex++)
        {
            for (var checkpointIndex = 0;
                checkpointIndex < script.Calls[callIndex].Frames.Count;
                checkpointIndex++)
            {
                locks.Release($"{sessionId}:{script.GetLockName(callIndex, checkpointIndex)}");
            }
        }

        string? capturedBackground = null;
        string ChangeBackground(string background)
        {
            capturedBackground = background;
            return "Background changed successfully.";
        }

        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId });
        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                (Func<string, string>)ChangeBackground,
                name: "change_background",
                description: "Change the background."));
        });
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(status =>
        {
            if (status == ConversationStatus.AwaitingInput)
            {
                context.Turns[^1].ResponseBlocks
                    .OfType<UIActionBlock>()
                    .Single()
                    .InvokeAsync()
                    .GetAwaiter()
                    .GetResult();
            }
        });

        await context.SendMessageAsync("Change the background to something new");

        Assert.AreEqual("linear-gradient(135deg, #ff9a9e, #fad0c4)", capturedBackground);
        var finalText = context.Turns[^1].ResponseBlocks.OfType<RichContentBlock>().Single().RawText;
        Assert.AreEqual("Background changed to a sunset gradient.", finalText);
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

    private static ReplayCheckpointScript CreateSingleFrameScript()
        => new()
        {
            Calls = [CreateSingleFrameCall()],
        };

    private static ReplayCall CreateSingleFrameCall()
        => new()
        {
            Frames =
            [
                new ReplayFrame
                {
                    Name = "only-frame",
                    Updates = [new ChatResponseUpdate(ChatRole.Assistant, "hello back")],
                },
            ],
        };
}
