#if (GenerateApi)
using System.Net.Http;
#endif
#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;
#if (GenerateApi)
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;
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
#if (OrganizationalAuth || IndividualB2CAuth)
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
#endif
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

#if (GenerateApi)
    private readonly IDownstreamApi _downstreamApi;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,
                            IDownstreamApi downstreamApi)
    {
        _logger = logger;
        _downstreamApi = downstreamApi;
    }

#if (EnableOpenAPI)
    [HttpGet(Name = "GetWeatherForecast")]
#else
    [HttpGet]
#endif
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        using var response = await _downstreamApi.CallApiForUserAsync("DownstreamApi").ConfigureAwait(false);
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
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
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

#if (EnableOpenAPI)
    [HttpGet(Name = "GetWeatherForecast")]
#else
    [HttpGet]
#endif
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var user = await _graphServiceClient.Me.GetAsync();

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
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

#if (EnableOpenAPI)
    [HttpGet(Name = "GetWeatherForecast")]
#else
    [HttpGet]
#endif
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
#endif
}
