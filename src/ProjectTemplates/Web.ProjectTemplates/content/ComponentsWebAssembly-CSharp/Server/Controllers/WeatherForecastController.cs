#if (!NoAuth)
using Microsoft.AspNetCore.Authorization;
#endif
#if (GenerateApi)
using Microsoft.Identity.Web;
using System.Net;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif
using Microsoft.AspNetCore.Mvc;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web.Resource;
#endif
using ComponentsWebAssembly_CSharp.Shared;

namespace ComponentsWebAssembly_CSharp.Server.Controllers;

#if (!NoAuth)
[Authorize]
#endif
[ApiController]
[Route("[controller]")]
#if (OrganizationalAuth)
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
#elseif (IndividualB2CAuth)
[RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
#endif
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

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

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var user = await _graphServiceClient.Me.Request().GetAsync();

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

    [HttpGet]
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
