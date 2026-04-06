// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class FunctionApprovalBlockTests
{
    [Fact]
    public void CreatedWithPendingStatus()
    {
        var innerBlock = new FunctionInvocationContentBlock
        {
            Call = new FunctionCallContent("call-1", "DeleteFile", null)
        };
        var request = new ToolApprovalRequestContent("req-1", innerBlock.Call);
        var block = new FunctionApprovalBlock(innerBlock, request);

        Assert.Equal(ApprovalStatus.Pending, block.Status);
        Assert.Same(innerBlock, block.InnerBlock);
        Assert.Same(request, block.ApprovalRequest);
    }

    [Fact]
    public async Task Approve_SetsStatusAndSignalsResume()
    {
        var block = CreateBlock();

        block.Approve();

        Assert.Equal(ApprovalStatus.Approved, block.Status);
        var resultTask = block.GetResultAsync();
        Assert.True(resultTask.IsCompleted);
        var result = await resultTask;
        Assert.IsType<ToolApprovalResponseContent>(result);
    }

    [Fact]
    public void Reject_SetsStatusAndSignalsResume()
    {
        var block = CreateBlock();

        block.Reject("Not safe");

        Assert.Equal(ApprovalStatus.Rejected, block.Status);
        Assert.True(block.GetResultAsync().IsCompleted);
    }

    [Fact]
    public void Approve_FiresNotifyChanged()
    {
        var block = CreateBlock();
        var changed = false;
        block.OnChanged(() => changed = true);

        block.Approve();

        Assert.True(changed);
    }

    [Fact]
    public void Reject_FiresNotifyChanged()
    {
        var block = CreateBlock();
        var changed = false;
        block.OnChanged(() => changed = true);

        block.Reject();

        Assert.True(changed);
    }

    [Fact]
    public void Approve_WithoutAgentContext_Succeeds()
    {
        var block = CreateBlock();

        block.Approve();

        Assert.Equal(ApprovalStatus.Approved, block.Status);
        Assert.True(block.GetResultAsync().IsCompleted);
    }

    private static FunctionApprovalBlock CreateBlock()
    {
        var innerBlock = new FunctionInvocationContentBlock
        {
            Call = new FunctionCallContent("call-1", "DeleteFile", null)
        };
        var request = new ToolApprovalRequestContent("req-1", innerBlock.Call);
        return new FunctionApprovalBlock(innerBlock, request);
    }
}
