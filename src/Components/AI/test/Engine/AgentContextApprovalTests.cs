// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Engine;

public class AgentContextApprovalTests
{
    private static (UIAgent agent, DelegatingStreamingChatClient client) CreateAgent()
    {
        var client = new DelegatingStreamingChatClient();
        var agent = new UIAgent(client);
        return (agent, client);
    }

    [Fact]
    public async Task ApprovalBlock_SetsStatusToAwaitingInput()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitApprovalRequest("call-1", "DeleteFile"));
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            // Auto-approve when awaiting to unblock SendMessageAsync
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                var block = turn.ResponseBlocks.OfType<FunctionApprovalBlock>().Single();
                block.Approve();
            }
        });

        await context.SendMessageAsync("Delete the file");

        Assert.Contains(ConversationStatus.AwaitingInput, statuses);
    }

    [Fact]
    public async Task ApprovalBlock_EmittedAsFunctionApprovalBlock()
    {
        var (agent, client) = CreateAgent();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitApprovalRequest("call-1", "DeleteFile");
            }
            return ResponseEmitters.EmitTextResponse("Done");
        });
        var context = new AgentContext(agent);

        FunctionApprovalBlock? capturedBlock = null;
        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                capturedBlock = turn.ResponseBlocks.OfType<FunctionApprovalBlock>().First();
                capturedBlock.Approve();
            }
        });

        await context.SendMessageAsync("Delete the file");

        Assert.NotNull(capturedBlock);
        Assert.Equal(ApprovalStatus.Approved, capturedBlock!.Status);
        Assert.Equal("DeleteFile", capturedBlock.InnerBlock.ToolName);
    }

    [Fact]
    public async Task Approve_ResumesStreamingThenIdle()
    {
        var (agent, client) = CreateAgent();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitApprovalRequest("call-1", "DeleteFile");
            }
            return ResponseEmitters.EmitTextResponse("File deleted successfully.");
        });
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                turn.ResponseBlocks.OfType<FunctionApprovalBlock>().Single().Approve();
            }
        });

        await context.SendMessageAsync("Delete the file");

        Assert.Equal(ConversationStatus.Idle, context.Status);

        // Verify the status transitions: Streaming → AwaitingInput → Streaming → Idle
        Assert.Equal(new[]
        {
            ConversationStatus.Streaming,
            ConversationStatus.AwaitingInput,
            ConversationStatus.Streaming,
            ConversationStatus.Idle,
        }, statuses);
    }

    [Fact]
    public async Task Reject_SetsStatusToIdle()
    {
        var (agent, client) = CreateAgent();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitApprovalRequest("call-1", "DeleteFile");
            }
            return ResponseEmitters.EmitTextResponse("Operation cancelled.");
        });
        var context = new AgentContext(agent);

        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                turn.ResponseBlocks.OfType<FunctionApprovalBlock>().Single().Reject("Not safe");
            }
        });

        await context.SendMessageAsync("Delete the file");

        Assert.Equal(ConversationStatus.Idle, context.Status);
        var turn = Assert.Single(context.Turns);
        var approvalBlock = turn.ResponseBlocks.OfType<FunctionApprovalBlock>().Single();
        Assert.Equal(ApprovalStatus.Rejected, approvalBlock.Status);
    }

    [Fact]
    public async Task Approve_ContinuationBlocksAddedToSameTurn()
    {
        var (agent, client) = CreateAgent();
        var callCount = 0;
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitApprovalRequest("call-1", "DeleteFile");
            }
            return ResponseEmitters.EmitTextResponse("Done!");
        });
        var context = new AgentContext(agent);

        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                var turn = context.Turns[^1];
                turn.ResponseBlocks.OfType<FunctionApprovalBlock>().Single().Approve();
            }
        });

        await context.SendMessageAsync("Delete the file");

        // Same turn should have both the approval block and the continuation text
        Assert.Single(context.Turns);
        var turn = context.Turns[0];
        var textBlock = turn.ResponseBlocks.OfType<RichContentBlock>().SingleOrDefault();
        Assert.NotNull(textBlock);
        Assert.Equal("Done!", textBlock!.RawText);
    }

    [Fact]
    public async Task StatusTransitions_NoApproval_RemainsStreamingToIdle()
    {
        var (agent, client) = CreateAgent();
        client.SetHandler((msgs, opts, ct) =>
            ResponseEmitters.EmitTextResponse("Just text"));
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s => statuses.Add(s));

        await context.SendMessageAsync("Hello");

        Assert.Equal(new[] { ConversationStatus.Streaming, ConversationStatus.Idle }, statuses);
    }
}
