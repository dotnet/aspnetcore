// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorUnitedApp.Data;

namespace BlazorUnitedApp.Pages;

public partial class FetchDataCore
{
#if !BROWSER
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private async Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
    {
        await Task.Delay(1000);

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
#endif

// -----------------------------
// Everything below here would be codegenerated based on the method signatures above
#if !BROWSER
    public static void MapEndpoints(WebApplication application)
    {
        application.MapGet("/fetchdata/api/GetForecastAsync", (string startDate) =>
        {
            var param_startDate = System.Text.Json.JsonSerializer.Deserialize<DateTime>(startDate);

            var instance = new FetchDataCore(); // TODO: Resolve via DI
            return instance.GetForecastAsync(param_startDate);
        });
    }
#else
    [Microsoft.AspNetCore.Components.Inject]
    private HttpClient? _generated_HttpClient { get; set; }

    private Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
    {
        var param_startDate = System.Uri.EscapeDataString(
            System.Text.Json.JsonSerializer.Serialize(startDate));
        return System.Net.Http.Json.HttpClientJsonExtensions.GetFromJsonAsync<WeatherForecast[]>(
            _generated_HttpClient!,
            $"fetchdata/api/GetForecastAsync?startDate={param_startDate}");
    }
#endif
}
