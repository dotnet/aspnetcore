// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using HostedBlazorWebassemblyApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HostedBlazorWebassemblyApp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
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
