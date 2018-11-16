// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;

namespace HttpClientFactorySample
{
    public class Program
    {
        public static void Main(string[] args) => Run().GetAwaiter().GetResult();

        public static async Task Run()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(b =>
            {
                b.AddFilter((category, level) => true); // Spam the world with logs.

                // Add console logger so we can see all the logging produced by the client by default.
                b.AddConsole(c => c.IncludeScopes = true);
            });

            Configure(serviceCollection);

            var services = serviceCollection.BuildServiceProvider();

            Console.WriteLine("Creating a client...");
            var github = services.GetRequiredService<GitHubClient>();

            Console.WriteLine("Sending a request...");
            var response = await github.GetJson();

            var data = await response.Content.ReadAsAsync<JObject>();
            Console.WriteLine("Response data:");
            Console.WriteLine(data);

            Console.WriteLine("Press the ANY key to exit...");
            Console.ReadKey();
        }

        public static void Configure(IServiceCollection services)
        {
            var registry = services.AddPolicyRegistry();

            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
            var longTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

            registry.Add("regular", timeout);
            registry.Add("long", longTimeout);

            services.AddHttpClient("github", c =>
            {
                c.BaseAddress = new Uri("https://api.github.com/");

                c.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json"); // GitHub API versioning
                c.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample"); // GitHub requires a user-agent
            })

            // Build a totally custom policy using any criteria
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)))

            // Use a specific named policy from the registry. Simplest way, policy is cached for the
            // lifetime of the handler.
            .AddPolicyHandlerFromRegistry("regular")

            // Run some code to select a policy based on the request
            .AddPolicyHandler((request) =>
            {
                return request.Method == HttpMethod.Get ? timeout : longTimeout;
            })

            // Run some code to select a policy from the registry based on the request
            .AddPolicyHandlerFromRegistry((reg, request) =>
            {
                return request.Method == HttpMethod.Get ?
                    reg.Get<IAsyncPolicy<HttpResponseMessage>>("regular") :
                    reg.Get<IAsyncPolicy<HttpResponseMessage>>("long");
            })
            
            // Build a policy that will handle exceptions, 408s, and 500s from the remote server
            .AddTransientHttpErrorPolicy(p => p.RetryAsync())

            .AddHttpMessageHandler(() => new RetryHandler()) // Retry requests to github using our retry handler
            .AddTypedClient<GitHubClient>();
        }

        private class GitHubClient
        {
            public GitHubClient(HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public HttpClient HttpClient { get; }

            // Gets the list of services on github.
            public async Task<HttpResponseMessage> GetJson()
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/");

                var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                return response;
            }
        }

        private class RetryHandler : DelegatingHandler
        {
            public int RetryCount { get; set; } = 5;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                for (var i = 0; i < RetryCount; i++)
                {
                    try
                    {
                        return await base.SendAsync(request, cancellationToken);
                    }
                    catch (HttpRequestException) when (i == RetryCount - 1)
                    {
                        throw;
                    }
                    catch (HttpRequestException)
                    {
                        // Retry
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                    }
                }

                // Unreachable.
                throw null;
            }
        }
    }
}
