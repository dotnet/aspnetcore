using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Company.WebApplication1.Controllers
{
#if (!NoAuth)
    [Authorize]
#endif

    [Route("api/SampleData/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly ILogger<WeatherController> logger;

        public WeatherController(ILogger<WeatherController> _logger)
        {
            logger = _logger;
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
        public TemperatureUnit TemperatureUnit { get; set; }
        public string Location { get; set; }
    }
}
