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
#if (GenerateApi)
public class WeatherForecastController(IDownstreamApi downstreamApi) : ControllerBase
#elseif (GenerateGraph)
public class WeatherForecastController(GraphServiceClient graphServiceClient) : ControllerBase
#else
public class WeatherForecastController : ControllerBase
#endif
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

#if (GenerateApi)
#if (EnableOpenAPI)
    [HttpGet(Name = "GetWeatherForecast")]
#else
    [HttpGet]
#endif
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        using var response = await downstreamApi.CallApiForUserAsync("DownstreamApi").ConfigureAwait(false);
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
#if (EnableOpenAPI)
    [HttpGet(Name = "GetWeatherForecast")]
#else
    [HttpGet]
#endif
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var user = await graphServiceClient.Me.GetAsync();

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
#else
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
