using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web.Resource;
#endif
using ComponentsWebAssembly_CSharp.Shared;

namespace ComponentsWebAssembly_CSharp.Server.Controllers
{
#if (!NoAuth)
    [Authorize]
#endif
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
#if (OrganizationalAuth || IndividualB2CAuth)

        // The Web API will only accept tokens 1) for users, and 2) having the "api-scope" scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "api-scope" };
#endif

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
#if (OrganizationalAuth || IndividualB2CAuth)
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

#endif
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
