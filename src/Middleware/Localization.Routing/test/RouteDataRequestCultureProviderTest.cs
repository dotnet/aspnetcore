// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Localization.Routing
{
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
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
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
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
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
            var builder = new WebHostBuilder()
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

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(requestUrl);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var data = await response.Content.ReadAsStringAsync();
                Assert.Equal($"{expectedCulture},{expectedUICulture}", data);
            }
        }
    }
}
