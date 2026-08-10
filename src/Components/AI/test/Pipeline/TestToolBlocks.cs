// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

[ToolBlock("get_weather")]
public partial class WeatherToolBlock : FunctionInvocationContentBlock
{
    [ToolParameter]
    public string? Location { get; set; }

    [ToolParameter(Name = "units")]
    public string? TemperatureUnits { get; set; }
}

[ToolBlock("search")]
public partial class SearchToolBlock : FunctionInvocationContentBlock
{
    [ToolParameter(Name = "q")]
    public string? Query { get; set; }

    [ToolParameter]
    public int MaxResults { get; set; }
}
