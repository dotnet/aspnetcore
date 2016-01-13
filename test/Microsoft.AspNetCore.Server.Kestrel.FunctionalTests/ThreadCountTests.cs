// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ThreadCountTests
    {
        public async Task ZeroToTenThreads(int threadCount)
        {
            var port = PortManager.GetPort();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", $"http://localhost:{port}/" }
                })
                .Build();

            var hostBuilder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(app =>
                {
                    var serverInfo = app.ServerFeatures.Get<IKestrelServerInformation>();
                    serverInfo.ThreadCount = threadCount;
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello World");
                    });
                });            

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    // Send 20 requests just to make sure we don't get any failures
                    var requestTasks = new List<Task<string>>();
                    for (int i = 0; i < 20; i++)
                    {
                        var requestTask = client.GetStringAsync($"http://localhost:{port}/");
                        requestTasks.Add(requestTask);
                    }
                    
                    foreach (var result in await Task.WhenAll(requestTasks))
                    {
                        Assert.Equal("Hello World", result);
                    }
                }
            }
        }

        public static TheoryData<int> OneToTen
        {
            get
            {
                var dataset = new TheoryData<int>();
                for (int i = 1; i <= 10; i++)
                {
                    dataset.Add(i);
                }
                return dataset;
            }
        }
    }
}
