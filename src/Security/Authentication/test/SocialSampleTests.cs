// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SocialSample;

namespace Microsoft.AspNetCore.Authentication;

public class SocialSampleTests
{
    [Fact]
    public async Task ErrorEndpointHtmlEncodesFailureMessage()
    {
        using var host = await CreateHost();
        using var client = host.GetTestClient();

        var failureMessage = "<h1>test</h1>";
        var response = await client.GetAsync($"/error?FailureMessage={Uri.EscapeDataString(failureMessage)}");
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(HtmlEncoder.Default.Encode(failureMessage), responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(failureMessage, responseBody, StringComparison.Ordinal);
    }

    private static async Task<IHost> CreateHost()
    {
        var configuration = new Dictionary<string, string>
        {
            ["facebook:appid"] = "facebook-app-id",
            ["facebook:appsecret"] = "facebook-app-secret",
            ["google:clientid"] = "google-client-id",
            ["google:clientsecret"] = "google-client-secret",
            ["twitter:consumerkey"] = "twitter-consumer-key",
            ["twitter:consumersecret"] = "twitter-consumer-secret",
            ["microsoftaccount:clientid"] = "microsoft-client-id",
            ["microsoftaccount:clientsecret"] = "microsoft-client-secret",
            ["github:clientid"] = "github-client-id",
            ["github:clientsecret"] = "github-client-secret",
            ["github-token:clientid"] = "github-token-client-id",
            ["github-token:clientsecret"] = "github-token-client-secret",
        };

        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(configuration));
            })
            .Build();

        await host.StartAsync();

        return host;
    }
}
