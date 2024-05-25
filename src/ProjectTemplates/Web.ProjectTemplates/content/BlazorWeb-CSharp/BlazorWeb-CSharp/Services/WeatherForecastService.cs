using BlazorWeb_CSharp.Client;

namespace BlazorWeb_CSharp.Services;

internal class WeatherForecastService
{
    public async Task<WeatherForecast[]?> GetWeatherForecastAsync()
    {
        // Simulate asynchronous loading
        await Task.Delay(500);

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast(startDate.AddDays(index), Random.Shared.Next(-20, 55), summaries[Random.Shared.Next(summaries.Length)])).ToArray();
    }
}
