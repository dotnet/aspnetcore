// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class PredictiveStateTests
{
    [Theory]
    [InlineData(true, "Complete draft")]
    [InlineData(false, "Original")]
    public async Task Confirmation_AcceptsOrRejectsPredictiveStateAndContinues(
        bool accepted,
        string expectedDocument)
    {
        var callCount = 0;
        List<ChatMessage>? continuationMessages = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitPredictionWithConfirmation(cancellationToken);
            }

            continuationMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse("Reviewed.", cancellationToken);
        });

        UIAgent<DocumentState> agent = null!;

        string ConfirmChanges(bool accepted)
        {
            if (accepted)
            {
                agent.State.AcceptPredictiveState();
            }
            else
            {
                agent.State.RejectPredictiveState();
            }

            return accepted ? "Accepted." : "Rejected.";
        }

        agent = CreateAgent(client, ConfirmChanges);
        using (agent)
        using (var context = new AgentContext(agent))
        {
            var documents = new List<string>();
            using var stateSubscription = agent.State.OnChanged(
                () => documents.Add(agent.State.Value.Document));
            using var statusSubscription = context.RegisterOnStatusChanged(status =>
            {
                if (status == ConversationStatus.AwaitingInput)
                {
                    var action = context.Turns[^1].ResponseBlocks
                        .OfType<UIActionBlock>()
                        .Single();
                    action.Call.Arguments ??= new Dictionary<string, object?>();
                    action.Call.Arguments["accepted"] = accepted;
                    _ = action.InvokeAsync();
                }
            });

            await context.SendMessageAsync("Edit the document");

            Assert.Equal(["Draft", "Complete draft", expectedDocument], documents);
            Assert.Equal(expectedDocument, agent.State.Value.Document);
            Assert.False(agent.State.HasPendingPredictiveState);
            Assert.Equal(2, callCount);
            Assert.NotNull(continuationMessages);
            var result = Assert.IsType<FunctionResultContent>(
                Assert.Single(continuationMessages.Last().Contents));
            Assert.Equal(accepted ? "Accepted." : "Rejected.", result.Result?.ToString());
        }
    }

    [Fact]
    public async Task CancelAsync_RollsBackPredictiveState()
    {
        var stateObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            EmitPredictionUntilCanceled(stateObserved, cancellationToken));
        using var agent = CreateAgent(client);
        using var context = new AgentContext(agent);

        var sendTask = context.SendMessageAsync("Edit the document");
        await stateObserved.Task;
        await context.CancelAsync();
        await sendTask;

        Assert.Equal("Original", agent.State.Value.Document);
        Assert.False(agent.State.HasPendingPredictiveState);
        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Empty(Assert.Single(context.Turns).ResponseBlocks);
    }

    [Fact]
    public async Task CallerCancellation_RollsBackPredictiveStateAndCancelsTask()
    {
        var stateObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            EmitPredictionUntilCanceled(stateObserved, cancellationToken));
        using var agent = CreateAgent(client);
        using var context = new AgentContext(agent);
        using var cancellationSource = new CancellationTokenSource();

        var sendTask = context.SendMessageAsync(
            "Edit the document",
            cancellationSource.Token);
        await stateObserved.Task;
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);
        Assert.Equal("Original", agent.State.Value.Document);
        Assert.False(agent.State.HasPendingPredictiveState);
        Assert.Equal(ConversationStatus.Idle, context.Status);
    }

    [Fact]
    public async Task CompletedTurnWithoutConfirmation_RollsBackPredictiveState()
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            EmitPredictionWithoutConfirmation(cancellationToken));
        using var agent = CreateAgent(client);
        using var context = new AgentContext(agent);

        await context.SendMessageAsync("Edit the document");

        Assert.Equal("Original", agent.State.Value.Document);
        Assert.False(agent.State.HasPendingPredictiveState);
        Assert.Equal(ConversationStatus.Idle, context.Status);
    }

    [Fact]
    public async Task RestoreAsync_RejectsRestoredPredictiveState()
    {
        var thread = new InMemoryConversationThread("thread-1");
        thread.AppendUserMessage(new ChatMessage(ChatRole.User, "Edit the document"));
        thread.AppendUpdate(CreateStateUpdate("Rejected draft"));
        thread.CompleteTurn();
        var client = new DelegatingStreamingChatClient();
        using var agent = CreateAgent(client, thread: thread);

        await agent.RestoreAsync();

        Assert.Equal("Original", agent.State.Value.Document);
        Assert.False(agent.State.HasPendingPredictiveState);
    }

    private static UIAgent<DocumentState> CreateAgent(
        IChatClient client,
        Func<bool, string>? confirm = null,
        IConversationThread? thread = null)
    {
        return new UIAgent<DocumentState>(client, options =>
        {
            options.Thread = thread;
            options.StateMapper = context =>
            {
                if (context.Update.RawRepresentation is not DocumentState state)
                {
                    return;
                }

                context.SetPredictiveState(state);
            };
            if (confirm is not null)
            {
                options.RegisterUIAction(AIFunctionFactory.Create(
                    confirm,
                    "confirm_changes",
                    "Confirm the document changes."));
            }
        }, new DocumentState { Document = "Original" });
    }

    private static async IAsyncEnumerable<ChatResponseUpdate>
        EmitPredictionWithConfirmation(
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CreateStateUpdate("Draft");
        yield return CreateStateUpdate("Complete draft");
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "confirmation-message",
            Contents = [new FunctionCallContent("confirmation-call", "confirm_changes")],
            FinishReason = ChatFinishReason.ToolCalls,
        };
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitPredictionUntilCanceled(
        TaskCompletionSource stateObserved,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return CreateStateUpdate("Draft");
        stateObserved.SetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate>
        EmitPredictionWithoutConfirmation(
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CreateStateUpdate("Unconfirmed draft");
        await Task.CompletedTask;
    }

    private static ChatResponseUpdate CreateStateUpdate(string document) => new()
    {
        Role = ChatRole.Assistant,
        RawRepresentation = new DocumentState { Document = document },
    };

    private sealed class DocumentState
    {
        public string Document { get; set; } = "";
    }
}
