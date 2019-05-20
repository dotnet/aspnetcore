using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;

namespace Company.WebApplication1.Controllers
{
#if (!NoAuth)
    [Authorize]
#endif

    [Route("api/SampleData/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        [HttpGet]
        public ActionResult<WeatherResult> GetWeatherResult(string location)
        {
            var rng = new Random();
            return new WeatherResult
            {
                Location = location,
                Temperature = rng.Next(-20, 55),
                TemperatureUnit = TemperatureUnit.Celsius
            };
        }

        [HttpGet]
        public ActionResult<WeatherResult> GetWeatherForecasts(string location, TemperatureUnit unit)
        {
            var rng = new Random();
            return new WeatherResult
            {
                Location = location,
                Temperature = rng.Next(-20, 55),
                TemperatureUnit = unit
            };
        }
    }

    public enum TemperatureUnit
    {
        Celsius,
        Fahrenheit
    }
    public class WeatherResult
    {
        public int Temperature { get; set; }
        public TemperatureUnit Temperature { get; set; }
        public string Location { get; set; }
    }
}
