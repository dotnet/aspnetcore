// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class FunctionApprovalBlock : InteractiveFunctionBlock, IInteractiveBlock
{
    private readonly TaskCompletionSource<AIContent> _tcs = new();

    internal FunctionApprovalBlock(
        FunctionInvocationContentBlock innerBlock,
        ToolApprovalRequestContent request)
        : base(innerBlock)
    {
        ApprovalRequest = request;
        Status = ApprovalStatus.Pending;
    }

    public ApprovalStatus Status { get; private set; }

    public ToolApprovalRequestContent ApprovalRequest { get; }

    public void Approve()
    {
        Status = ApprovalStatus.Approved;
        var response = ApprovalRequest.CreateResponse(approved: true);
        NotifyChanged();
        _tcs.TrySetResult(response);
    }

    public void Reject(string? reason = null)
    {
        Status = ApprovalStatus.Rejected;
        var response = ApprovalRequest.CreateResponse(approved: false);
        NotifyChanged();
        _tcs.TrySetResult(response);
    }

    public Task<AIContent> GetResultAsync(CancellationToken cancellationToken = default)
        => _tcs.Task.WaitAsync(cancellationToken);
}
