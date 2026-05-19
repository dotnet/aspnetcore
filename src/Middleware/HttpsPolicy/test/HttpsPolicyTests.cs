// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpsPolicy.Tests;

public class HttpsPolicyTests
{
    [Theory]
    [InlineData(302, 443, 2592000, false, false, "max-age=2592000", "https://localhost/")]
    [InlineData(301, 5050, 2592000, false, false, "max-age=2592000", "https://localhost:5050/")]
    [InlineData(301, 443, 2592000, false, false, "max-age=2592000", "https://localhost/")]
    [InlineData(301, 443, 2592000, true, false, "max-age=2592000; includeSubDomains", "https://localhost/")]
    [InlineData(301, 443, 2592000, false, true, "max-age=2592000; preload", "https://localhost/")]
    [InlineData(301, 443, 2592000, true, true, "max-age=2592000; includeSubDomains; preload", "https://localhost/")]
    [InlineData(302, 5050, 2592000, true, true, "max-age=2592000; includeSubDomains; preload", "https://localhost:5050/")]
    public async Task SetsBothHstsAndHttpsRedirection_RedirectOnFirstRequest_HstsOnSecondRequest(int statusCode, int? tlsPort, int maxAge, bool includeSubDomains, bool preload, string expectedHstsHeader, string expectedUrl)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.Configure<HttpsRedirectionOptions>(options =>
                    {
                        options.RedirectStatusCode = statusCode;
                        options.HttpsPort = tlsPort;
                    });
                    services.Configure<HstsOptions>(options =>
                    {
                        options.IncludeSubDomains = includeSubDomains;
                        options.MaxAge = TimeSpan.FromSeconds(maxAge);
                        options.Preload = preload;
                        options.ExcludedHosts.Clear(); // allowing localhost for testing
                    });
                })
                .Configure(app =>
                {
                    app.UseHttpsRedirection();
                    app.UseHsts();
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync("Hello world");
                    });
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        server.Features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "");

        var response = await client.SendAsync(request);

        Assert.Equal(statusCode, (int)response.StatusCode);
        Assert.Equal(expectedUrl, response.Headers.Location.ToString());

        client = server.CreateClient();
        client.BaseAddress = new Uri(response.Headers.Location.ToString());
        request = new HttpRequestMessage(HttpMethod.Get, expectedUrl);
        response = await client.SendAsync(request);

        Assert.Equal(expectedHstsHeader, response.Headers.GetValues(HeaderNames.StrictTransportSecurity).FirstOrDefault());
    }
}
