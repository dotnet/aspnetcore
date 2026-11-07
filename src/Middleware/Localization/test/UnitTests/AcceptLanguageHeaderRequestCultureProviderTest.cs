// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Localization;

public class AcceptLanguageHeaderRequestCultureProviderTest
{
    [Fact]
    public async Task GetFallbackLanguage_ReturnsFirstNonNullCultureFromSupportedCultureList()
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
                                new CultureInfo("ar-SA"),
                                new CultureInfo("en-US")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("jp,ar-SA,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(3, count);
        }
    }

    [Fact]
    public async Task GetFallbackLanguage_ReturnsFromSupportedCulture_AcceptLanguageListContainsSupportedCultures()
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
                        DefaultRequestCulture = new RequestCulture("fr-FR"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA"),
                                new CultureInfo("en-US")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-SA,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
        }
    }

    [Fact]
    public async Task GetFallbackLanguage_ReturnsDefault_AcceptLanguageListDoesnotContainSupportedCultures()
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
                        DefaultRequestCulture = new RequestCulture("fr-FR"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA"),
                                new CultureInfo("af-ZA")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("fr-FR", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-MA,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(3, count);
        }
    }

    [Fact]
    public async Task OmitDefaultRequestCultureShouldNotThrowNullReferenceException_And_ShouldGetTheRightCulture()
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
                                new CultureInfo("ar-YE")
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

                        Assert.Equal("ar-YE", requestCulture.Culture.Name);
                        Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-YE,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(3, count);
        }
    }

    [Theory]
    [InlineData("en-US", 3, "en-US")]
    [InlineData("fr;q=0.5,en-US,de;q=0.8", 3, "en-US,de,fr")]
    [InlineData("fr;q=0.5,en-US;q=0.5,de;q=0.5", 3, "fr,en-US,de")]
    [InlineData("fr;q=0.1,en-US;q=0.2,de;q=0.3,es;q=1.0", 3, "de,en-US,fr")]
    [InlineData("fr;q=0.1,en-US;q=0.2,de;q=0.3,es;q=1.0", 0, "es,de,en-US,fr")]
    [InlineData("*,fr;q=0.5,en", 3, "en,*,fr")]
    [InlineData("a;q=0.1,b;q=0.2,c;q=0.3,d;q=0.4,e;q=0.5,f;q=0.6,g;q=0.7,h;q=0.8,i;q=0.9,j,k;q=0.9,l;q=0.8,m;q=0.7,n;q=0.6,o;q=0.5,p;q=0.4,q;q=0.3,r;q=0.2", 0,
        "j,i,k,h,l,g,m,f,n,e,o,d,p,c,q,b,r,a")]
    [InlineData("a;q=0.1,b;q=0.2,c;q=0.3,d;q=0.4,e;q=0.5,f;q=0.6,g;q=0.7,h;q=0.8,i;q=0.9,j,k;q=0.9,l;q=0.8,m;q=0.7,n;q=0.6,o;q=0.5,p;q=0.4,q;q=0.3,r;q=0.2", 16,
        "j,i,k,h,l,g,m,f,n,e,o,d,p,c,b,a")]
    public async Task DetermineProviderCultureResult_OrdersLanguagesByQuality(string acceptLanguage, int maximumValuesToTry, string expected)
    {
        var provider = new AcceptLanguageHeaderRequestCultureProvider
        {
            MaximumAcceptLanguageHeaderValuesToTry = maximumValuesToTry
        };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.AcceptLanguage = acceptLanguage;

        var result = await provider.DetermineProviderCultureResult(httpContext);

        Assert.NotNull(result);
        Assert.Equal(expected.Split(','), result.Cultures.Select(c => c.Value));
        Assert.Equal(expected.Split(','), result.UICultures.Select(c => c.Value));
    }
}
