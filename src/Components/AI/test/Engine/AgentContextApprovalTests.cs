// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextApprovalTests
{
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "User declined")]
    public async Task Approval_ContinuesWithUserResponse(
        bool approved,
        string? reason)
    {
        var callCount = 0;
        List<ChatMessage>? continuationMessages = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((messages, options, cancellationToken) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return EmitApprovalRequest(cancellationToken);
            }

            continuationMessages = messages.ToList();
            return ResponseEmitters.EmitTextResponse(
                approved ? "Approved." : "Rejected.",
                cancellationToken);
        });
        using var agent = new UIAgent(client);
        using var context = new AgentContext(agent);
        var statuses = new List<ConversationStatus>();
        using var subscription = context.RegisterOnStatusChanged(status =>
        {
            statuses.Add(status);
            if (status == ConversationStatus.AwaitingInput)
            {
                var block = context.Turns[^1].ResponseBlocks
                    .OfType<FunctionApprovalBlock>()
                    .Single();
                if (approved)
                {
                    block.Approve();
                }
                else
                {
                    block.Reject(reason);
                }
            }
        });

        await context.SendMessageAsync("Delete the report");

        Assert.Equal(2, callCount);
        Assert.Equal(
            [
                ConversationStatus.Streaming,
                ConversationStatus.AwaitingInput,
                ConversationStatus.Streaming,
                ConversationStatus.Idle,
            ],
            statuses);

        var continuation = Assert.IsType<ChatMessage>(continuationMessages?.LastOrDefault());
        Assert.Equal(ChatRole.User, continuation.Role);
        var response = Assert.IsType<ToolApprovalResponseContent>(
            Assert.Single(continuation.Contents));
        Assert.Equal(approved, response.Approved);
        Assert.Equal(reason, response.Reason);
        Assert.Equal("approval-call-1", response.ToolCall.CallId);

        var turn = Assert.Single(context.Turns);
        Assert.Single(turn.ResponseBlocks.OfType<FunctionApprovalBlock>());
        Assert.Equal(
            approved ? "Approved." : "Rejected.",
            Assert.Single(turn.ResponseBlocks.OfType<RichContentBlock>()).RawText);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitApprovalRequest(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var call = new FunctionCallContent(
            "approval-call-1",
            "delete_report",
            new Dictionary<string, object?> { ["path"] = "report.tmp" });
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "approval-message-1",
            Contents = [new ToolApprovalRequestContent("approval-request-1", call)],
        };
        await Task.CompletedTask;
    }
}
