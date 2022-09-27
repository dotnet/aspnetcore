// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace ComponentsApp.Server;

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
