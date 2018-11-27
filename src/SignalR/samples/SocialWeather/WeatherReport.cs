// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SocialWeather
{
    public enum Weather { Sunny, MostlySunny, PartlySunny, PartlyCloudy, MostlyCloudy, Cloudy }

    public class WeatherReport
    {
        public int Temperature { get; set; }

        public long ReportTime { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Weather Weather { get; set; }

        public string ZipCode { get; set; }
    }
}
