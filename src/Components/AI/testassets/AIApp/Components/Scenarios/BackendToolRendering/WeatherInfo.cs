// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AIApp.Components.Scenarios.BackendToolRendering;

internal sealed class WeatherInfo
{
    public int Temperature { get; set; }
    public string Conditions { get; set; } = "";
    public int Humidity { get; set; }
    public int WindSpeed { get; set; }
    public int FeelsLike { get; set; }
}
