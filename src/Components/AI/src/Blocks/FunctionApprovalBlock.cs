// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a function invocation that requires approval before the conversation can continue.
/// </summary>
/// <example>
/// <code>
/// if (block.Status == ApprovalStatus.Pending)
/// {
///     block.Approve();
/// }
/// </code>
/// </example>
public class FunctionApprovalBlock : ContentBlock, IInteractiveBlock
{
    private readonly TaskCompletionSource<AIContent> _resultSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _responseLock = new();

    internal FunctionApprovalBlock(
        ContentBlock innerBlock,
        ToolApprovalRequestContent request)
    {
        ArgumentNullException.ThrowIfNull(innerBlock);
        InnerBlock = innerBlock;
        ApprovalRequest = request;
    }

    /// <summary>
    /// Gets the wrapped function invocation block.
    /// </summary>
    public ContentBlock InnerBlock { get; }

    /// <summary>
    /// Gets the function call represented by this block.
    /// </summary>
    public FunctionCallContent? Call => InnerBlock switch
    {
        FunctionInvocationContentBlock invocation => invocation.Call,
        UIActionBlock action => action.Call,
        _ => null,
    };

    /// <summary>
    /// Gets the function result represented by this block.
    /// </summary>
    public FunctionResultContent? Result => InnerBlock switch
    {
        FunctionInvocationContentBlock invocation => invocation.Result,
        UIActionBlock action => action.Result,
        _ => null,
    };

    /// <summary>
    /// Gets the name of the invoked tool.
    /// </summary>
    public string? ToolName => Call?.Name;

    /// <summary>
    /// Gets the arguments supplied to the tool.
    /// </summary>
    public IDictionary<string, object?>? Arguments => Call?.Arguments;

    /// <summary>
    /// Gets a value indicating whether the server produced a result.
    /// </summary>
    public bool HasResult => Result is not null;

    /// <summary>
    /// Gets the current approval status.
    /// </summary>
    public ApprovalStatus Status { get; private set; }

    /// <summary>
    /// Gets the approval request received from the chat client.
    /// </summary>
    public ToolApprovalRequestContent ApprovalRequest { get; }

    /// <summary>
    /// Approves the function invocation. Only the first response is applied.
    /// </summary>
    public void Approve()
    {
        Respond(ApprovalStatus.Approved, reason: null);
    }

    /// <summary>
    /// Rejects the function invocation. Only the first response is applied.
    /// </summary>
    /// <param name="reason">An optional explanation for the rejection.</param>
    public void Reject(string? reason = null)
    {
        Respond(ApprovalStatus.Rejected, reason);
    }

    /// <inheritdoc />
    public Task<AIContent> GetResultAsync(CancellationToken cancellationToken = default)
        => _resultSource.Task.WaitAsync(cancellationToken);

    private void Respond(ApprovalStatus status, string? reason)
    {
        lock (_responseLock)
        {
            if (Status != ApprovalStatus.Pending)
            {
                return;
            }

            Status = status;
            var response = ApprovalRequest.CreateResponse(
                approved: status == ApprovalStatus.Approved,
                reason);
            _resultSource.SetResult(response);
        }

        NotifyChanged();
    }
}
