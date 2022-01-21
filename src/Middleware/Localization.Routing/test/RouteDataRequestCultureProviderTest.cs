// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Localization.Routing;

public class RouteDataRequestCultureProviderTest
{
    [Theory]
    [InlineData("{culture}/{ui-culture}/hello", "ar-SA/ar-YE/hello", "ar-SA", "ar-YE")]
    [InlineData("{CULTURE}/{UI-CULTURE}/hello", "ar-SA/ar-YE/hello", "ar-SA", "ar-YE")]
    [InlineData("{culture}/{ui-culture}/hello", "unsupported/unsupported/hello", "en-US", "en-US")]
    [InlineData("{culture}/hello", "ar-SA/hello", "ar-SA", "en-US")]
    [InlineData("hello", "hello", "en-US", "en-US")]
    [InlineData("{ui-culture}/hello", "ar-YE/hello", "en-US", "ar-YE")]
    public async Task GetCultureInfo_FromRouteData(
        string routeTemplate,
        string requestUrl,
        string expectedCulture,
        string expectedUICulture)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRouter(routes =>
                    {
                        routes.MapMiddlewareRoute(routeTemplate, fork =>
                        {
                            var options = new RequestLocalizationOptions
                            {
                                DefaultRequestCulture = new RequestCulture("en-US"),
                                SupportedCultures = new List<CultureInfo>
                                {
                                        new CultureInfo("ar-SA")
                                },
                                SupportedUICultures = new List<CultureInfo>
                                {
                                        new CultureInfo("ar-YE")
                                }
                            };
                            options.RequestCultureProviders = new[]
                            {
                                    new RouteDataRequestCultureProvider()
                                    {
                                        Options = options
                                    }
                            };
                            fork.UseRequestLocalization(options);

                            fork.Run(context =>
                            {
                                var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                                var requestCulture = requestCultureFeature.RequestCulture;
                                return context.Response.WriteAsync(
                                    $"{requestCulture.Culture.Name},{requestCulture.UICulture.Name}");
                            });
                        });
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(requestUrl);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync();
            Assert.Equal($"{expectedCulture},{expectedUICulture}", data);
        }
    }

    [Fact]
    public async Task GetDefaultCultureInfo_IfCultureKeysAreMissing()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var options = new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US")
                    };
                    options.RequestCultureProviders = new[]
                    {
                            new RouteDataRequestCultureProvider()
                            {
                                Options = options
                            }
                    };
                    app.UseRequestLocalization(options);
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;

                        return context.Response.WriteAsync(
                                    $"{requestCulture.Culture.Name},{requestCulture.UICulture.Name}");
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync();
            Assert.Equal("en-US,en-US", data);
        }
    }

    [Theory]
    [InlineData("{c}/{uic}/hello", "ar-SA/ar-YE/hello", "ar-SA", "ar-YE")]
    [InlineData("{C}/{UIC}/hello", "ar-SA/ar-YE/hello", "ar-SA", "ar-YE")]
    [InlineData("{c}/hello", "ar-SA/hello", "ar-SA", "en-US")]
    [InlineData("hello", "hello", "en-US", "en-US")]
    [InlineData("{uic}/hello", "ar-YE/hello", "en-US", "ar-YE")]
    public async Task GetCultureInfo_FromRouteData_WithCustomKeys(
        string routeTemplate,
        string requestUrl,
        string expectedCulture,
        string expectedUICulture)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRouter(routes =>
                    {
                        routes.MapMiddlewareRoute(routeTemplate, fork =>
                        {
                            var options = new RequestLocalizationOptions
                            {
                                DefaultRequestCulture = new RequestCulture("en-US"),
                                SupportedCultures = new List<CultureInfo>
                                {
                                        new CultureInfo("ar-SA")
                                },
                                SupportedUICultures = new List<CultureInfo>
                                {
                                        new CultureInfo("ar-YE")
                                }
                            };
                            options.RequestCultureProviders = new[]
                            {
                                    new RouteDataRequestCultureProvider()
                                    {
                                        Options = options,
                                        RouteDataStringKey = "c",
                                        UIRouteDataStringKey = "uic"
                                    }
                            };
                            fork.UseRequestLocalization(options);

                            fork.Run(context =>
                            {
                                var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                                var requestCulture = requestCultureFeature.RequestCulture;

                                return context.Response.WriteAsync(
                                    $"{requestCulture.Culture.Name},{requestCulture.UICulture.Name}");
                            });
                        });
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync(requestUrl);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync();
            Assert.Equal($"{expectedCulture},{expectedUICulture}", data);
        }
    }
}
