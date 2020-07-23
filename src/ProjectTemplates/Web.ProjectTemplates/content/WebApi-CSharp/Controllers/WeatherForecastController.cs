using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
#if (GenerateApi)
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Net;
using System.Net.Http;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web.Resource;
#endif

namespace Company.WebApplication1.Controllers
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

        // The Web API will only accept tokens 1) for users, and 2) having the access_as_user scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "access_as_user" };

#if (GenerateApi)
        private readonly IDownstreamWebApi _downstreamWebApi;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
                              IDownstreamWebApi downstreamWebApi)
        {
             _logger = logger;
            _downstreamWebApi = downstreamWebApi;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

            string downstreamApiResult = await _downstreamWebApi.CallWebApiAsync();

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

#elseif (GenerateGraph)
        private readonly GraphServiceClient _graphServiceClient;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
                                         GraphServiceClient graphServiceClient)
        {
             _logger = logger;
            _graphServiceClient = graphServiceClient;
       }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
            var user = await _graphServiceClient.Me.Request().GetAsync();

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
#else
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
#endif
    }
}
