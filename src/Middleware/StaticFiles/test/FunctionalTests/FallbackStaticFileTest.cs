// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.StaticFiles
{
    public class FallbackStaticFileTest : LoggedTest
    {
        [Fact]
        public async Task ReturnsFileForDefaultPattern()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(LoggerFactory);
                })
                .UseKestrel()
                .UseWebRoot(AppContext.BaseDirectory)
                .Configure(app =>
                {
                    var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.Map("/hello", context =>
                        {
                            return context.Response.WriteAsync("Hello, world!");
                        });

                        endpoints.MapFallbackToFile("default.html", new StaticFileOptions()
                        {
                            FileProvider = new PhysicalFileProvider(Path.Combine(environment.WebRootPath, "SubFolder")),
                        });
                    });
                });

            using (var server = builder.Start(TestUrlHelper.GetTestUrl(ServerType.Kestrel)))
            {
                var environment = server.Services.GetRequiredService<IWebHostEnvironment>();
                using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(server)) })
                {
                    var response = await client.GetAsync("hello");
                    var responseText = await response.Content.ReadAsStringAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("Hello, world!", responseText);

                    response = await client.GetAsync("/");
                    var responseContent = await response.Content.ReadAsByteArrayAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    AssertFileEquals(environment, "SubFolder/default.html", responseContent);
                }
            }
        }

        [Fact]
        public async Task ReturnsFileForCustomPattern()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(LoggerFactory);
                })
                .UseKestrel()
                .UseWebRoot(AppContext.BaseDirectory)
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.Map("/hello", context =>
                        {
                            return context.Response.WriteAsync("Hello, world!");
                        });

                        endpoints.MapFallbackToFile("/prefix/{*path:nonfile}", "TestDocument.txt");
                    });
                });

            using (var server = builder.Start(TestUrlHelper.GetTestUrl(ServerType.Kestrel)))
            {
                var environment = server.Services.GetRequiredService<IWebHostEnvironment>();
                using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(server)) })
                {
                    var response = await client.GetAsync("hello");
                    var responseText = await response.Content.ReadAsStringAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("Hello, world!", responseText);

                    response = await client.GetAsync("prefix/Some-Path");
                    var responseContent = await response.Content.ReadAsByteArrayAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    AssertFileEquals(environment, "TestDocument.txt", responseContent);
                }
            }
        }

        private static void AssertFileEquals(IWebHostEnvironment environment, string filePath, byte[] responseContent)
        {
            var fileInfo = environment.WebRootFileProvider.GetFileInfo(filePath);
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);

            using (var stream = fileInfo.CreateReadStream())
            {
                var fileContents = new byte[stream.Length];
                stream.Read(fileContents, 0, (int)stream.Length);
                Assert.True(responseContent.SequenceEqual(fileContents));
            }
        }
    }
}
