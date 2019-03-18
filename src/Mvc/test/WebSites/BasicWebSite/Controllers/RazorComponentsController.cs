// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class RazorComponentsController : Controller
    {
        private static WeatherRow[] _weatherData = new[]
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

        [HttpGet("/components")]
        [HttpGet("/components/{component}")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/WeatherData")]
        [Produces("application/json")]
        public IActionResult WeatherData()
        {
            return Ok(_weatherData);
        }

        private class WeatherRow
        {
            public string DateFormatted { get; internal set; }
            public int TemperatureC { get; internal set; }
            public string Summary { get; internal set; }
            public int TemperatureF { get; internal set; }
        }
    }
}
