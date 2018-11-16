// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Performance
{
    [ParameterizedJobConfig(typeof(CoreConfig))]
    public class CreationOverheadBenchmark
    {
        private const int Iterations = 100;

        public CreationOverheadBenchmark()
        {
            Handler = new FakeClientHandler();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("example", c =>
            {
                c.BaseAddress = new Uri("http://example.com/");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => Handler);

            var services = serviceCollection.BuildServiceProvider();
            Factory = services.GetRequiredService<IHttpClientFactory>();
        }

        public IHttpClientFactory Factory { get; }

        public HttpMessageHandler Handler { get; }

        [Benchmark(
            Description = "use IHttpClientFactory", 
            OperationsPerInvoke = Iterations)]
        public async Task CreateClient()
        {
            for (var i = 0; i < Iterations; i++)
            {
                var client = Factory.CreateClient("example");

                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/Products"));
                response.EnsureSuccessStatusCode();
            }
        }

        [Benchmark(
            Description = "new HttpClient",
            Baseline = true,
            OperationsPerInvoke = Iterations)]
        public async Task Baseline()
        {
            for (var i = 0; i < Iterations; i++)
            {
                var client = new HttpClient(Handler, disposeHandler: false)
                {
                    BaseAddress = new Uri("http://example.com/"),
                    DefaultRequestHeaders =
                    {
                        Accept =
                        {
                            new MediaTypeWithQualityHeaderValue("application/json"),
                        }
                    },
                };

                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/Products"));
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
