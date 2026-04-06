// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ToolBlockAttribute : Attribute
{
    public string ToolName { get; }

    public ToolBlockAttribute(string toolName)
    {
        ToolName = toolName;
    }
}
