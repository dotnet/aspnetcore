// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a server-side function invocation and its eventual result.
/// </summary>
/// <remarks>
/// Custom content blocks that represent function invocations should derive from this type
/// and populate <see cref="Call"/>. This keeps the call, tool name, and arguments available
/// when other features, such as function approval, wrap the custom block.
/// </remarks>
/// <example>
/// <code>
/// public sealed class WeatherContentBlock : FunctionInvocationContentBlock
/// {
///     public string? Location { get; set; }
/// }
/// </code>
/// </example>
public class FunctionInvocationContentBlock : ContentBlock
{
    private FunctionCallContent? _call;

    /// <summary>
    /// Gets or sets the function call represented by this block.
    /// </summary>
    public FunctionCallContent? Call
    {
        get => _call;
        set
        {
            _call = value;
            if (value is not null)
            {
                Id = value.CallId;
            }
        }
    }

    /// <summary>
    /// Gets or sets the function result paired with <see cref="Call"/>.
    /// </summary>
    public FunctionResultContent? Result { get; set; }

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
