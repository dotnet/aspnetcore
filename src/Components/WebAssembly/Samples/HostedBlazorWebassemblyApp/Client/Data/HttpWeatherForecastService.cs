// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HostedBlazorWebassemblyApp.Shared;

namespace HostedBlazorWebassemblyApp.Client.Data;

public class HttpWeatherForecastService : IWeatherForecastService
{
    public HttpWeatherForecastService(HttpClient client)
    {
        Client = client;
    }

    public HttpClient Client { get; }

    public async Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
    {
        return (await Client.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast"))!;
    }
}
