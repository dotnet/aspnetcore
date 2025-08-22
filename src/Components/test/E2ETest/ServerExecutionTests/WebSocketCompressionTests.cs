// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.RegularExpressions;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests;

public abstract partial class AllowedWebSocketCompressionTests(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{
    public string ExpectedPolicy { get; set; }

    [Fact]
    public async Task EmbeddingServerAppInsideIframe_WorksAsync()
    {
        Navigate("/subdir/iframe");

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.Empty(logs);

        // Get the iframe element from the page, and inspect its contents for a p element with id inside-iframe
        var iframe = Browser.FindElement(By.TagName("iframe"));
        Browser.SwitchTo().Frame(iframe);
        Browser.Exists(By.Id("inside-iframe"));

        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var response = await client.GetAsync("/subdir/iframe");
        response.EnsureSuccessStatusCode();

        if (ExpectedPolicy != null)
        {
            var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
            Assert.Equal($"frame-ancestors {ExpectedPolicy}", csp);
        }
        else
        {
            Assert.DoesNotContain("Content-Security-Policy", response.Headers.Select(h => h.Key));
        }
    }

    [Fact]
    public async Task EmbeddingServerAppInsideIframe_WorksWithMultipleCspHeaders()
    {
        Navigate("/subdir/iframe?add-csp");

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.Empty(logs);

        // Get the iframe element from the page, and inspect its contents for a p element with id inside-iframe
        var iframe = Browser.FindElement(By.TagName("iframe"));
        Browser.SwitchTo().Frame(iframe);
        Browser.Exists(By.Id("inside-iframe"));

        using var client = new HttpClient() { BaseAddress = _serverFixture.RootUri };
        var response = await client.GetAsync("/subdir/iframe?add-csp");
        response.EnsureSuccessStatusCode();

        if (ExpectedPolicy != null)
        {
            Assert.Equal(
                [
                    "script-src 'self' 'unsafe-inline'",
                    $"frame-ancestors {ExpectedPolicy}"
                ],
                response.Headers.GetValues("Content-Security-Policy"));
        }
        else
        {
            Assert.Equal(
                [
                    "script-src 'self' 'unsafe-inline'"
                ],
                response.Headers.GetValues("Content-Security-Policy"));
        }
    }
}

public abstract partial class BlockedWebSocketCompressionTests(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{
    [Fact]
    public void EmbeddingServerAppInsideIframe_WithCompressionEnabled_Fails()
    {
        Navigate("/subdir/iframe");

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Matches(ParseErrorMessageRegex, logs[0].Message);
    }

    [GeneratedRegex(@"security - Refused to frame 'http://\d+\.\d+\.\d+\.\d+:\d+/' because an ancestor violates the following Content Security Policy directive: ""frame-ancestors 'none'"".")]
    private static partial Regex ParseErrorMessageRegex { get; }
}

public partial class DefaultConfigurationWebSocketCompressionTests : AllowedWebSocketCompressionTests
{
    public DefaultConfigurationWebSocketCompressionTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output) : base(browserFixture, serverFixture, output)
    {
        ExpectedPolicy = "'self'";
    }
}

public partial class CustomConfigurationCallbackWebSocketCompressionTests : AllowedWebSocketCompressionTests
{
    public CustomConfigurationCallbackWebSocketCompressionTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output) : base(browserFixture, serverFixture, output)
    {
        ExpectedPolicy = "'self'";
        serverFixture.UpdateHostServices = services =>
        {
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            // Callback wins over setting.
            configuration.IsCompressionDisabled = true;
            configuration.ConfigureWebSocketAcceptContext = (context, acceptContext) =>
            {
                acceptContext.DangerousEnableCompression = true;
                return Task.CompletedTask;
            };
        };
    }
}

public partial class CompressionDisabledWebSocketCompressionTests : AllowedWebSocketCompressionTests
{
    public CompressionDisabledWebSocketCompressionTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output) : base(
        browserFixture, serverFixture, output)
    {
        serverFixture.UpdateHostServices = services =>
        {
            // Ensures that the policy does not get applied when compression is disabled and
            // no callback is set.
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            configuration.IsCompressionDisabled = true;
            configuration.CspPolicy = "'none'";
        };
    }
}

public partial class NoneAncestorWebSocketCompressionTests : BlockedWebSocketCompressionTests
{
    public NoneAncestorWebSocketCompressionTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        // Ensures the policy gets applied whenever compression is enabled, which is the default.
        serverFixture.UpdateHostServices = services =>
        {
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            configuration.CspPolicy = "'none'";
        };
    }
}

public partial class NoneAncestorWebSocketAppliesPolicyOnCallbackCompressionTests : BlockedWebSocketCompressionTests
{
    public NoneAncestorWebSocketAppliesPolicyOnCallbackCompressionTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.UpdateHostServices = services =>
        {
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            // Ensures that the policy gets applied whenever the callback is set, even if
            // the compression is disabled via the property.
            configuration.IsCompressionDisabled = true;
            configuration.ConfigureWebSocketAcceptContext = (context, acceptContext) =>
            {
                acceptContext.DangerousEnableCompression = true;
                return Task.CompletedTask;
            };
            configuration.CspPolicy = "'none'";
        };
    }
}

