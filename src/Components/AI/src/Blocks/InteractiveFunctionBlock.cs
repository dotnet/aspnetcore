// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Base class for an interactive block that wraps a function invocation.
/// </summary>
public abstract class InteractiveFunctionBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveFunctionBlock"/> class.
    /// </summary>
    /// <param name="innerBlock">The function invocation represented by this block.</param>
    protected InteractiveFunctionBlock(FunctionInvocationContentBlock innerBlock)
    {
        ArgumentNullException.ThrowIfNull(innerBlock);
        InnerBlock = innerBlock;
    }

    /// <summary>
    /// Gets the wrapped function invocation block.
    /// </summary>
    public FunctionInvocationContentBlock InnerBlock { get; }

    /// <summary>
    /// Gets the function call represented by this block.
    /// </summary>
    public FunctionCallContent? Call => InnerBlock.Call;

    /// <summary>
    /// Gets the function result represented by this block.
    /// </summary>
    public FunctionResultContent? Result => InnerBlock.Result;

    /// <summary>
    /// Gets the name of the invoked tool.
    /// </summary>
    public string? ToolName => InnerBlock.ToolName;

    /// <summary>
    /// Gets the arguments supplied to the tool.
    /// </summary>
    public IDictionary<string, object?>? Arguments => InnerBlock.Arguments;

    /// <summary>
    /// Gets a value indicating whether the server produced a result.
    /// </summary>
    public bool HasResult => InnerBlock.HasResult;
}
