// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNet.Server.Kestrel.FunctionalTests
{
    public class ReuseStreamsTests
    {
        [Fact]
        public async Task ReuseStreamsOn()
        {
            var streamCount = 0;
            var loopCount = 20;
            Stream lastStream = null;
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", "http://localhost:8801/" },
                    { "kestrel.reuseStreams", "true" }
                })
                .Build();

            var builder = new WebApplicationBuilder()
                .UseConfiguration(config)
                .UseServerFactory("Microsoft.AspNet.Server.Kestrel")
                .Configure(app =>
                {
                    var serverInfo = app.ServerFeatures.Get<IKestrelServerInformation>();
                    app.Run(context =>
                    {
                        if (context.Request.Body != lastStream)
                        {
                            lastStream = context.Request.Body;
                            streamCount++;
                        }
                        return context.Request.Body.CopyToAsync(context.Response.Body);
                    });
                });            

            using (var app = builder.Build().Start())
            {
                using (var client = new HttpClient())
                {
                    for (int i = 0; i < loopCount; i++)
                    {
                        var content = $"{i} Hello World {i}";
                        var request = new HttpRequestMessage()
                        {
                            RequestUri = new Uri("http://localhost:8801/"),
                            Method = HttpMethod.Post,
                            Content = new StringContent(content)
                        };
                        request.Headers.Add("Connection", new string[] { "Keep-Alive" });
                        var responseMessage = await client.SendAsync(request);
                        var result = await responseMessage.Content.ReadAsStringAsync();
                        Assert.Equal(content, result);
                    }
                }
            }

            Assert.True(streamCount < loopCount);
        }

        [Fact]
        public async Task ReuseStreamsOff()
        {
            var streamCount = 0;
            var loopCount = 20;
            Stream lastStream = null;
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "server.urls", "http://localhost:8802/" },
                    { "kestrel.reuseStreams", "false" }
                })
                .Build();

            var hostBuilder = new WebApplicationBuilder()
                .UseConfiguration(config)
                .UseServerFactory("Microsoft.AspNet.Server.Kestrel")
                .Configure(app =>
                {
                    var serverInfo = app.ServerFeatures.Get<IKestrelServerInformation>();
                    app.Run(context =>
                    {
                        if (context.Request.Body != lastStream)
                        {
                            lastStream = context.Request.Body;
                            streamCount++;
                        }
                        return context.Request.Body.CopyToAsync(context.Response.Body);
                    });
                });

            using (var app = hostBuilder.Build().Start())
            {
                using (var client = new HttpClient())
                {
                    for (int i = 0; i < loopCount; i++)
                    {
                        var content = $"{i} Hello World {i}";
                        var request = new HttpRequestMessage()
                        {
                            RequestUri = new Uri("http://localhost:8802/"),
                            Method = HttpMethod.Post,
                            Content = new StringContent(content)
                        };
                        request.Headers.Add("Connection", new string[] { "Keep-Alive" });
                        var responseMessage = await client.SendAsync(request);
                        var result = await responseMessage.Content.ReadAsStringAsync();
                        Assert.Equal(content, result);
                    }
                }
            }

            Assert.Equal(loopCount, streamCount);
        }
    }
}
