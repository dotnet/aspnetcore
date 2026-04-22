// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Samples;

public class S04_ToolApprovalTest
{
    [Fact]
    public async Task Approval_Required_PausesAndResumeOnApprove()
    {
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitApprovalRequest(
                    "call-1", "DeleteFile",
                    new Dictionary<string, object?> { ["path"] = "/tmp/data.csv" });
            }
            return ResponseEmitters.EmitTextResponse("File deleted successfully.");
        });

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        var statuses = new List<ConversationStatus>();
        context.RegisterOnStatusChanged(s =>
        {
            statuses.Add(s);
            if (s == ConversationStatus.AwaitingInput)
            {
                var block = context.Turns[^1].ResponseBlocks
                    .OfType<FunctionApprovalBlock>().Single();
                Assert.Equal(ApprovalStatus.Pending, block.Status);
                Assert.Equal("DeleteFile", block.ToolName);
                block.Approve();
            }
        });

        await context.SendMessageAsync("Delete /tmp/data.csv");

        Assert.Equal(ConversationStatus.Idle, context.Status);
        Assert.Equal(new[]
        {
            ConversationStatus.Streaming,
            ConversationStatus.AwaitingInput,
            ConversationStatus.Streaming,
            ConversationStatus.Idle,
        }, statuses);

        var textBlock = context.Turns[0].ResponseBlocks.OfType<RichContentBlock>().Single();
        Assert.Contains("deleted", textBlock.RawText);
    }

    [Fact]
    public async Task Approval_Rejected_CompletesWithoutExecution()
    {
        var callCount = 0;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return ResponseEmitters.EmitApprovalRequest("call-1", "SendEmail");
            }
            return ResponseEmitters.EmitTextResponse("Understood, email not sent.");
        });

        var agent = new UIAgent(client);
        var context = new AgentContext(agent);

        context.RegisterOnStatusChanged(s =>
        {
            if (s == ConversationStatus.AwaitingInput)
            {
                context.Turns[^1].ResponseBlocks
                    .OfType<FunctionApprovalBlock>().Single()
                    .Reject("User declined");
            }
        });

        await context.SendMessageAsync("Send the email");

        Assert.Equal(ConversationStatus.Idle, context.Status);

        var approvalBlock = context.Turns[0].ResponseBlocks
            .OfType<FunctionApprovalBlock>().Single();
        Assert.Equal(ApprovalStatus.Rejected, approvalBlock.Status);
    }
}
