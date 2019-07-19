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
    public class CreationOverheadBenchmark
    {
        private const int Iterations = 100;

        public CreationOverheadBenchmark()
        {
            Handler = new FakeClientHandler();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient<TypedClient>("example", c =>
            {
                c.BaseAddress = new Uri("http://example.com/");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => Handler);

            Services = serviceCollection.BuildServiceProvider();
            Factory = Services.GetRequiredService<IHttpClientFactory>();
        }

        public IServiceProvider Services { get; }

        public IHttpClientFactory Factory { get; }

        public HttpMessageHandler Handler { get; }

        [Benchmark(
            Description = "use IHttpClientFactory with named client", 
            OperationsPerInvoke = Iterations)]
        public async Task CreateNamedClient()
        {
            for (var i = 0; i < Iterations; i++)
            {
                var client = Factory.CreateClient("example");

                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/Products"));
                response.EnsureSuccessStatusCode();
            }
        }

        // This test is a super set of CreateNamedClient - because all of the work done in there
        // also has to happen to create a typed client. This is here for scenario comparison purposes,
        // It won't be possible to optimize this code path enough to have the same performance as CreateNamedClient.
        [Benchmark(
            Description = "use IHttpClientFactory with typed client", 
            OperationsPerInvoke = Iterations)]
        public async Task CreateTypeClient()
        {
            for (var i = 0; i < Iterations; i++)
            {
                var client = Services.GetRequiredService<TypedClient>();

                var response = await client.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/Products"));
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

        private class TypedClient
        {
            public TypedClient(HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public HttpClient HttpClient { get; }
        }
    }
}
