// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI;

namespace AIApp;

[ToolBlock("get_weather")]
public partial class WeatherToolBlock : FunctionInvocationContentBlock
{
    [ToolParameter]
    public string? Location { get; set; }

    [ToolParameter(Name = "units")]
    public string? TemperatureUnits { get; set; }
}
