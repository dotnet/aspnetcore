// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Localization;

public class RequestLocalizationMiddlewareTest
{
    [Fact]
    public async Task GetCultureInfoTraversesParentPropertyToResolveCulture()
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
                                new CultureInfo("zh-Hant")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("zh-Hant")
                        }
                    };
                    app.UseRequestLocalization(options);
                    app.Run(context =>
                    {
                        // NOTE: This test exploits the fact that zh-TW's parent culture (zh-Hant) is not
                        //       present in the string representation of the culture, thus proving that
                        //       the GetCultureInfo(...) method in RequestLocalizationMiddleware is
                        //       correctly traversing the Parent properties of CultureInfo instances
                        //       rather than trying to process the string.
                        //
                        //       The more modern equivalent of zh-TW is zh-Hant-TW but zh-TW exists for
                        //       legacy reasons.
                        //
                        //       Citation:
                        //       https://social.msdn.microsoft.com/Forums/en-US/8b93c07b-93bd-465f-b48f-0fff544c06d8/quotzhhansquot-vs-quotzhchsquot-and-quotzhhantquot-vs-quotzhchtquot?forum=microsofttranslator
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("zh-Hant", requestCulture.Culture.ToString());
                        Assert.Equal("zh-Hant", requestCulture.UICulture.ToString());
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?culture=zh-TW");
        }
    }

    [Fact]
    public async Task NotProvidingCultureReturnsDefault()
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
                    app.UseRequestLocalization(options);
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("en-US", requestCulture.Culture.ToString());
                        Assert.Equal("en-US", requestCulture.UICulture.ToString());
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/page?culture=");
        }
    }
}
