// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class FunctionApprovalBlockTests
{
    [Fact]
    public async Task Approve_OnlyFirstResponseWins()
    {
        var block = CreateBlock();
        var changes = 0;
        using var subscription = block.OnChanged(() => changes++);

        block.Approve();
        block.Reject("Too late");
        block.Approve();

        var response = Assert.IsType<ToolApprovalResponseContent>(
            await block.GetResultAsync());
        Assert.True(response.Approved);
        Assert.Null(response.Reason);
        Assert.Equal(ApprovalStatus.Approved, block.Status);
        Assert.Equal("call-1", response.ToolCall.CallId);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Reject_PreservesToolCallAndReason()
    {
        var block = CreateBlock();

        block.Reject("Not safe");

        var response = Assert.IsType<ToolApprovalResponseContent>(
            await block.GetResultAsync());
        Assert.False(response.Approved);
        Assert.Equal("Not safe", response.Reason);
        Assert.Equal(ApprovalStatus.Rejected, block.Status);
        Assert.Same(block.ApprovalRequest.ToolCall, response.ToolCall);
    }

    private static FunctionApprovalBlock CreateBlock()
    {
        var call = new FunctionCallContent(
            "call-1",
            "delete_file",
            new Dictionary<string, object?> { ["path"] = "report.tmp" });
        var request = new ToolApprovalRequestContent("request-1", call);
        return new FunctionApprovalBlock(
            new FunctionInvocationContentBlock { Call = call },
            request)
        {
            Id = call.CallId!
        };
    }
}
