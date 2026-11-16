// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace DojoClient.Components.Scenarios.BackendToolRendering;

public sealed class WeatherInfo
{
    [JsonPropertyName("temperature")]
    public int Temperature { get; set; }

    [JsonPropertyName("conditions")]
    public string Conditions { get; set; } = string.Empty;

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("wind_speed")]
    public int WindSpeed { get; set; }

    [JsonPropertyName("feelsLike")]
    public int FeelsLike { get; set; }
}
