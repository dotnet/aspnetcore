// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComponentsApp;

public abstract class WeatherForecastService
{
    public abstract Task<WeatherForecast[]> GetForecastAsync(DateTime startDate);
}
