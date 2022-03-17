// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SocialWeather;

public enum Weather { Sunny, MostlySunny, PartlySunny, PartlyCloudy, MostlyCloudy, Cloudy }

public class WeatherReport
{
    public int Temperature { get; set; }

    public long ReportTime { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public Weather Weather { get; set; }

    public string ZipCode { get; set; }
}
