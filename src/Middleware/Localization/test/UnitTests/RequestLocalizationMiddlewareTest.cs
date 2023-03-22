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
    [Theory]
    [InlineData("zh-Hans-CN")]
    [InlineData("zh-Hans")]
    [InlineData("zh-CN")]
    [InlineData("zh-Hant-TW")]
    [InlineData("zh-Hant")]
    [InlineData("zh-TW")]
    public async Task RequestLocalizationMiddleware_ShouldFallBackToParentCultures_RegradlessOfHyphenSeparatorCheck(string requestedCulture)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var supportedCultures = new[] { "ar", "en", "zh" };

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

                        Assert.Equal("zh", requestCulture.RequestCulture.Culture.Name);
                        Assert.Equal("zh", requestCulture.RequestCulture.UICulture.Name);

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
