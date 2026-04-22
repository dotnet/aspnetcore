// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestApp.Services;

namespace TestApp.E2E.Tests.ServiceOverrides;

class FakeWeatherService : IWeatherService
{
    public Task<WeatherForecast[]> GetForecastsAsync()
    {
        return Task.FromResult(new[]
        {
            new WeatherForecast
            {
                Date = new DateOnly(2025, 1, 1),
                TemperatureC = 42,
                Summary = "TestWeather"
            }
        });
    }
}
