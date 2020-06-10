using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Wasm.Authentication.Shared;

namespace Wasm.Authentication.Client
{
    public class WeatherForecastClient : IDisposable
    {
        private readonly HttpClient _client;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public WeatherForecastClient(HttpClient client)
        {
            _client = client;
        }

        public Task<WeatherForecast[]> GetForecastAsync() =>
            _client.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast", _cts.Token);

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
