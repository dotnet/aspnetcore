// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Extensions.Localization;

public class RequestLocalizationMiddlewareTest
{
    [Fact]
    public async Task RequestLocalizationMiddleware_DoesNotFallBackFromNonPredefinedCulture()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRequestLocalization(options =>
                        {
                            options.SetDefaultCulture("en-US")
                                .AddSupportedCultures("en-US", "fr-FR")
                                .AddSupportedUICultures("en-US", "fr-FR")
                                .AddInitialRequestCultureProvider(new CookieRequestCultureProvider
                                {
                                    CookieName = "Preferences"
                                });
                        });

                        app.Run(context =>
                        {
                            var requestCulture = context.Features.Get<IRequestCultureFeature>();

                            Assert.Equal("en-US", requestCulture.RequestCulture.Culture.Name);
                            Assert.Equal("en-US", requestCulture.RequestCulture.UICulture.Name);

                            return Task.CompletedTask;
                        });
                    });
            }).Build();

        await host.StartAsync();

        using var server = host.GetTestServer();
        var client = server.CreateClient();
        const string nonPredefinedCulture = "fr-FR-a0000001-b0000001-c0000001-d0000001-e0000001";
        client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue("Preferences", $"c={nonPredefinedCulture}|uic={nonPredefinedCulture}").ToString());

        var response = await client.GetAsync(string.Empty);

        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("zh-Hans-CN", "zh")]
    [InlineData("zh-Hans", "zh")]
    [InlineData("zh-CN", "zh")]
    [InlineData("zh-Hant-TW", "zh")]
    [InlineData("zh-Hant", "zh")]
    [InlineData("zh-TW", "zh")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-Hans-CN", "zh-Hans")]
    [InlineData("zh-Hant-TW", "zh-Hant")]
    [InlineData("zh-TW", "zh-Hant")]
    public async Task RequestLocalizationMiddleware_ShouldFallBackToParentCultures_RegradlessOfHyphenSeparatorCheck(string requestedCulture, string parentCulture)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var supportedCultures = new[] { "ar", "en", parentCulture };

                    app.UseRequestLocalization(options =>
                    {
                        options.AddSupportedCultures(supportedCultures)
                            .AddSupportedUICultures(supportedCultures)
                            .AddInitialRequestCultureProvider(new CookieRequestCultureProvider
                            {
                                CookieName = "Preferences"
                            });
                    });

                    app.Run(async context =>
                    {
                        var requestCulture = context.Features.Get<IRequestCultureFeature>();

                        Assert.Equal(parentCulture, requestCulture.RequestCulture.Culture.Name);
                        Assert.Equal(parentCulture, requestCulture.RequestCulture.UICulture.Name);

                        await Task.CompletedTask;
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue("Preferences", $"c={requestedCulture}|uic={requestedCulture}").ToString());

            var response = await client.GetAsync(string.Empty);

            response.EnsureSuccessStatusCode();
        }
    }
}
