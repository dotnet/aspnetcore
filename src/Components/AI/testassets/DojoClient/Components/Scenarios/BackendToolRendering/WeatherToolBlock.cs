// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI;

namespace DojoClient.Components.Scenarios.BackendToolRendering;

[ToolBlock("get_weather")]
public partial class WeatherToolBlock : FunctionInvocationContentBlock
{
    [ToolParameter(Name = "location")]
    public string? Location { get; set; }

    [ToolResult]
    public WeatherInfo? Weather { get; set; }
}
