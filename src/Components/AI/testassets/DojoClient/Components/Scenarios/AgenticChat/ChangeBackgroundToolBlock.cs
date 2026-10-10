// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI;

namespace DojoClient.Components.Scenarios.AgenticChat;

[ToolBlock("change_background")]
public partial class ChangeBackgroundToolBlock : FunctionInvocationContentBlock
{
    [ToolParameter(Name = "background")]
    public string? Background { get; set; }

    [ToolParameter(Name = "color")]
    public string? Color { get; set; }

    public string? Value => Background ?? Color;
}
