// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace BlazorUnitedApp.Data;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }

    private string GetDebuggerDisplay() =>
        $"{Date:yyyy-MM-dd}: {TemperatureC}°C ({TemperatureF}°F) - {Summary ?? "No summary"}";
}
