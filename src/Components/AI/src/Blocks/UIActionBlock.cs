// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class UIActionBlock : InteractiveFunctionBlock, IInteractiveBlock
{
    private readonly AIFunction _function;
    private readonly TaskCompletionSource<AIContent> _tcs = new();

    internal UIActionBlock(AIFunction function, FunctionInvocationContentBlock innerBlock)
        : base(innerBlock)
    {
        _function = function;
    }

    public bool IsComplete { get; private set; }

    public async Task InvokeAsync(CancellationToken cancellationToken = default)
    {
        var arguments = Call?.Arguments is not null ? new AIFunctionArguments(Call.Arguments) : null;
        var result = await _function.InvokeAsync(arguments, cancellationToken);
        var frc = new FunctionResultContent(Call!.CallId, result);
        InnerBlock.Result = frc;
        IsComplete = true;
        NotifyChanged();
        _tcs.TrySetResult(frc);
    }

    public Task<AIContent> GetResultAsync(CancellationToken cancellationToken = default)
        => _tcs.Task.WaitAsync(cancellationToken);
}
