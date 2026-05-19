// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class RazorComponentsController : Controller
{
    private static readonly WeatherRow[] _weatherData = new[]
    {
                new WeatherRow
                {
                    DateFormatted = "06/05/2018",
                    TemperatureC = 1,
                    Summary = "Freezing",
                    TemperatureF = 33
                },
                new WeatherRow
                {
                    DateFormatted = "07/05/2018",
                    TemperatureC = 14,
                    Summary = "Bracing",
                    TemperatureF = 57
                },
                new WeatherRow
                {
                    DateFormatted = "08/05/2018",
                    TemperatureC = -13,
                    Summary = "Freezing",
                    TemperatureF = 9
                },
                new WeatherRow
                {
                    DateFormatted = "09/05/2018",
                    TemperatureC = -16,
                    Summary = "Balmy",
                    TemperatureF = 4
                },
                new WeatherRow
                {
                    DateFormatted = "10/05/2018",
                    TemperatureC = 2,
                    Summary = "Chilly",
                    TemperatureF = 29
                }
            };

    [HttpGet("/components/{**slug}")]
    [HttpGet("/components/routable/{**slug}")]
    public IActionResult Index()
    {
        // Override the path so that the router finds the RoutedPage component
        // as the client router doesn't support optional parameters.
        Request.Path = Request.Path.StartsWithSegments("/components/routable") ?
            PathString.FromUriComponent("/components/routable") : Request.Path;

        return View();
    }

    [HttpGet("/WeatherData")]
    [Produces("application/json")]
    public IActionResult WeatherData()
    {
        return Ok(_weatherData);
    }

    [HttpGet("/components/Navigation")]
    public IActionResult Navigation()
    {
        return View();
    }

    private class WeatherRow
    {
        public string DateFormatted { get; internal set; }
        public int TemperatureC { get; internal set; }
        public string Summary { get; internal set; }
        public int TemperatureF { get; internal set; }
    }
}
