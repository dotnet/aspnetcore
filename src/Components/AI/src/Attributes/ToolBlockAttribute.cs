// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Associates a typed <see cref="FunctionInvocationContentBlock"/> with a tool name.
/// </summary>
/// <example>
/// <code>
/// [ToolBlock("get_weather")]
/// public partial class WeatherBlock : FunctionInvocationContentBlock;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ToolBlockAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ToolBlockAttribute"/>.
    /// </summary>
    /// <param name="toolName">The function name emitted by the model.</param>
    public ToolBlockAttribute(string toolName)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);
        ToolName = toolName;
    }

    /// <summary>
    /// Gets the function name associated with the block.
    /// </summary>
    public string ToolName { get; }
}
