// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorUnitedApp.Client.Data;

public class ClientWeatherForecastService
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<ClientWeatherForecast[]> GetForecastAsync(DateOnly startDate)
    {
        await Task.Yield();
        return await Task.FromResult(Enumerable.Range(1, 5).Select(index => new ClientWeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray());
    }
}
