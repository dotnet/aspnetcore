// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public abstract class InteractiveFunctionBlock : ContentBlock
{
    protected InteractiveFunctionBlock(FunctionInvocationContentBlock innerBlock)
    {
        InnerBlock = innerBlock;
    }

    public FunctionInvocationContentBlock InnerBlock { get; }

    public FunctionCallContent? Call => InnerBlock.Call;

    public FunctionResultContent? Result => InnerBlock.Result;

    public string? ToolName => InnerBlock.ToolName;

    public IDictionary<string, object?>? Arguments => InnerBlock.Arguments;

    public bool HasResult => InnerBlock.HasResult;
}
