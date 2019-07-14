using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace HeaderPropagationSample
{
    public class SampleHostedService : IHostedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HeaderPropagationProcessor _headerPropagationProcessor;
        private readonly ILogger _logger;

        public SampleHostedService(IHttpClientFactory httpClientFactory, HeaderPropagationProcessor headerPropagationProcessor, ILogger<SampleHostedService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _headerPropagationProcessor = headerPropagationProcessor ?? throw new ArgumentNullException(nameof(headerPropagationProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return DoWorkAsync();
        }

        private async Task DoWorkAsync()
        {
            _logger.LogInformation("Background Service is working.");

            _headerPropagationProcessor.ProcessRequest(new Dictionary<string, StringValues>());
            var client = _httpClientFactory.CreateClient("test");
            var result = await client.GetAsync("http://localhost:62013/forwarded");

            _logger.LogInformation("Background Service:\n{result}", result);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
