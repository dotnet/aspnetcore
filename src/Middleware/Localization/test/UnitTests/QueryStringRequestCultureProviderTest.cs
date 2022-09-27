// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Localization;

public class QueryStringRequestCultureProviderTest
{
    [Fact]
    public async Task GetCultureInfoFromQueryString()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
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
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?culture=ar-SA&ui-culture=ar-YE");
        }
    }

    [Fact]
    public async Task GetDefaultCultureInfoIfCultureKeysAreMissing()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US")
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("en-US", requestCulture.Culture.Name);
                        Assert.Equal("en-US", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page");
        }
    }

    [Fact]
    public async Task GetDefaultCultureInfoIfCultureIsInSupportedCultureList()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("en-US", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?culture=ar-XY&ui-culture=ar-SA");
        }
    }

    [Fact]
    public async Task GetDefaultCultureInfoIfUICultureIsNotInSupportedList()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("en-US", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?culture=ar-SA&ui-culture=ar-XY");
        }
    }

    [Fact]
    public async Task GetSameCultureInfoIfCultureKeyIsMissing()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        Assert.Equal("ar-SA", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?ui-culture=ar-SA");
        }
    }

    [Fact]
    public async Task GetSameCultureInfoIfUICultureKeyIsMissing()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        Assert.Equal("ar-SA", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?culture=ar-SA");
        }
    }

    [Fact]
    public async Task GetCultureInfoFromQueryStringWithCustomKeys()
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
                    var provider = new QueryStringRequestCultureProvider();
                    provider.QueryStringKey = "c";
                    provider.UIQueryStringKey = "uic";
                    options.RequestCultureProviders.Insert(0, provider);
                    app.UseRequestLocalization(options);
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?c=ar-SA&uic=ar-YE");
        }
    }

    [Fact]
    public async Task GetTheRightCultureInfoRegardlessOfCultureNameCasing()
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
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("FR")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("FR")
                        }
                    };
                    var provider = new QueryStringRequestCultureProvider();

                    provider.QueryStringKey = "c";
                    provider.UIQueryStringKey = "uic";
                    options.RequestCultureProviders.Insert(0, provider);
                    app.UseRequestLocalization(options);
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("fr", requestCulture.Culture.ToString());
                        Assert.Equal("fr", requestCulture.UICulture.ToString());
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?c=FR&uic=FR");
        }
    }
}
