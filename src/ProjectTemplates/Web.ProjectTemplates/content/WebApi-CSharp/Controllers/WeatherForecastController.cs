#if (GenerateApi)
using System.Net.Http;
#endif
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;
#if (GenerateApi)
using Microsoft.Identity.Web;
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web.Resource;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif

namespace Company.WebApplication1.Controllers;

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

        using var response = await _downstreamWebApi.CallWebApiForUserAsync("DownstreamApi").ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var apiResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            // Do something
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}: {error}");
        }

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
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

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
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
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
#endif
}
