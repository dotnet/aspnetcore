// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.StaticFiles;

public class FallbackStaticFileTest : LoggedTest
{
    [Fact]
    public async Task ReturnsFileForDefaultPattern()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(LoggerFactory);
                })
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
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
            }).Build();

        await host.StartAsync();

        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();
        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
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

    [Fact]
    public async Task ReturnsFileForCustomPattern()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(LoggerFactory);
                })
                .UseKestrel()
                .UseUrls(TestUrlHelper.GetTestUrl(ServerType.Kestrel))
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
            }).Build();

        await host.StartAsync();

        var environment = host.Services.GetRequiredService<IWebHostEnvironment>();
        using (var client = new HttpClient { BaseAddress = new Uri(Helpers.GetAddress(host)) })
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
