// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ThreadCountTests
    {
        [ConditionalTheory]
        [MemberData(nameof(OneToTen))]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Tests fail on OS X due to low file descriptor limit.")]
        public async Task OneToTenThreads(int threadCount)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseLibuv(options =>
                {
                    options.ThreadCount = threadCount;
                })
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
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
                        var requestTask = client.GetStringAsync($"http://127.0.0.1:{host.GetPort()}/");
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
