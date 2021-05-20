// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace ComponentsApp.Server
{
    public class DefaultWeatherForecastService : WeatherForecastService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public DefaultWeatherForecastService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public override Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "sample-data", "weather.json");
            using (var file = File.OpenText(path))
            using (var reader = new JsonTextReader(file))
            {
                var serializer = new JsonSerializer();
                var forecasts = serializer.Deserialize<WeatherForecast[]>(reader);

                for (var i = 0; i < forecasts.Length; i++)
                {
                    forecasts[i].DateFormatted = startDate.AddDays(i).ToShortDateString();
                }

                return Task.FromResult(forecasts);
            }
        }
    }
}
