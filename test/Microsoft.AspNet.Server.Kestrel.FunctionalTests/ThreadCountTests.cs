// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNet.Server.Kestrel.FunctionalTests
{
    public class ThreadCountTests
    {
        [Theory(Skip = "https://github.com/aspnet/KestrelHttpServer/issues/232"), MemberData(nameof(OneToTen))]
        public async Task ZeroToTenThreads(int threadCount)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", "http://localhost:8790/" }
                })
                .Build();

            var hostBuilder = new WebHostBuilder(CallContextServiceLocator.Locator.ServiceProvider, config);
            hostBuilder.UseServer("Microsoft.AspNet.Server.Kestrel");
            hostBuilder.UseStartup(app =>
            {
                var serverInfo = app.ServerFeatures.Get<IKestrelServerInformation>();
                serverInfo.ThreadCount = threadCount;
                app.Run(context =>
                {
                    return context.Response.WriteAsync("Hello World");
                });
            });            

            using (var app = hostBuilder.Build().Start())
            {
                using (var client = new HttpClient())
                {
                    // Send 20 requests just to make sure we don't get any failures
                    var requestTasks = new List<Task<string>>();
                    for (int i = 0; i < 20; i++)
                    {
                        var requestTask = client.GetStringAsync("http://localhost:8790/");
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
