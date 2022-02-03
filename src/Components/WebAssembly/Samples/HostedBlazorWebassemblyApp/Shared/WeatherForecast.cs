// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace HostedBlazorWebassemblyApp.Shared;

public class WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public string Summary { get; set; } = "";

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
