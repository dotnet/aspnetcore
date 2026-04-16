// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestApp.Services;

public interface IWeatherService
{
    Task<WeatherForecast[]> GetForecastsAsync();
}
