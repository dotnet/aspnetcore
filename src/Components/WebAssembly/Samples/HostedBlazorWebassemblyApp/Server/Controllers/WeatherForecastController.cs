using System;
using System.Threading.Tasks;
using HostedBlazorWebassemblyApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HostedBlazorWebassemblyApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly IWeatherForecastService _forecastService;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IWeatherForecastService forecastService)
        {
            _forecastService = forecastService;
            _logger = logger;
        }

        [HttpGet]
        public Task<WeatherForecast[]> Get()
        {
            return _forecastService.GetForecastAsync(DateTime.Now);
        }
    }
}
