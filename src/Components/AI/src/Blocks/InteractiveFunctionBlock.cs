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
    protected InteractiveFunctionBlock(ContentBlock innerBlock)
    {
        ArgumentNullException.ThrowIfNull(innerBlock);
        InnerBlock = innerBlock;
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
        InteractiveFunctionBlock interactive => interactive.Call,
        _ => null,
    };

    /// <summary>
    /// Gets the function result represented by this block.
    /// </summary>
    public FunctionResultContent? Result => InnerBlock switch
    {
        FunctionInvocationContentBlock invocation => invocation.Result,
        UIActionBlock action => action.Result,
        InteractiveFunctionBlock interactive => interactive.Result,
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
}
