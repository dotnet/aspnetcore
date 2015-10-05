// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNet.Server.Kestrel.FunctionalTests
{
    public class RequestTests
    {
        [Fact(Skip = "https://github.com/aspnet/KestrelHttpServer/issues/234")]
        public async Task LargeUpload()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", "http://localhost:8791/" }
                })
                .Build();

            var hostBuilder = new WebHostBuilder(CallContextServiceLocator.Locator.ServiceProvider, config);
            hostBuilder.UseServer("Microsoft.AspNet.Server.Kestrel");
            hostBuilder.UseStartup(app =>
            {
                app.Run(async context =>
                {
                    // Read the full request body
                    var total = 0;
                    var bytes = new byte[1024];
                    var count = await context.Request.Body.ReadAsync(bytes, 0, bytes.Length);
                    while (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            Assert.Equal(total % 256, bytes[i]);
                            total++;
                        }
                        count = await context.Request.Body.ReadAsync(bytes, 0, bytes.Length);
                    }

                    await context.Response.WriteAsync(total.ToString(CultureInfo.InvariantCulture));
                });
            });            

            using (var app = hostBuilder.Build().Start())
            {
                using (var client = new HttpClient())
                {
                    var bytes = new byte[1024 * 1024];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = (byte)i;
                    }
                        
                    var response = await client.PostAsync("http://localhost:8791/", new ByteArrayContent(bytes));
                    response.EnsureSuccessStatusCode();
                    var sizeString = await response.Content.ReadAsStringAsync();
                    Assert.Equal(sizeString, bytes.Length.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
