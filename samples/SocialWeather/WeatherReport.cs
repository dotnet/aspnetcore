using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialWeather
{
    public enum Weather { Sunny, MostlySunny, PartlySunny, PartlyCloudy, MostlyCloudy, Cloudy }

    public class WeatherReport
    {
        public int Temperature { get; set; }

        public long ReportTime { get; set; }

        public Weather Weather { get; set; }

        public string ZipCode { get; set; }
    }
}
