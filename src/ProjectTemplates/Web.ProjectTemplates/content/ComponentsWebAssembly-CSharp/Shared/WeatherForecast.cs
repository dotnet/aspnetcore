using System;
using System.Collections.Generic;
using System.Text;

namespace ComponentsWebAssembly_CSharp.Shared
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

#if (!Nullable)
        public string Summary { get; set; }
#else
        public string? Summary { get; set; }
#endif

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
