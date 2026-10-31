// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using LocalizationSample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Localization.FunctionalTests;

public class LocalizationSampleTest
{
    [Fact]
    public async Task LocalizationSampleSmokeTest()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .UseStartup(typeof(Startup));
            }).Build();

        await host.StartAsync();

        var testHost = host.GetTestServer();
        var locale = "fr-FR";
        var client = testHost.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "My/Resources");
        var cookieValue = $"c={locale}|uic={locale}";
        request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Bonjour</h1>", await response.Content.ReadAsStringAsync());
    }
}
