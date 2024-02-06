// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
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
    [Fact]
    public void EmbeddingServerAppInsideIframe_Works()
    {
        Navigate("/subdir/iframe");

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.Empty(logs);

        // Get the iframe element from the page, and inspect its contents for a p element with id inside-iframe
        var iframe = Browser.FindElement(By.TagName("iframe"));
        Browser.SwitchTo().Frame(iframe);
        Browser.Exists(By.Id("inside-iframe"));
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

        Assert.Matches(ParseErrorMessage(), logs[0].Message);
    }

    [GeneratedRegex(@"security - Refused to frame 'http://\d+\.\d+\.\d+\.\d+:\d+/' because an ancestor violates the following Content Security Policy directive: ""frame-ancestors 'none'"".")]
    private static partial Regex ParseErrorMessage();
}

public partial class DefaultConfigurationWebSocketCompressionTests(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
    ITestOutputHelper output)
    : AllowedWebSocketCompressionTests(browserFixture, serverFixture, output)
{
}

public partial class CustomConfigurationCallbackWebSocketCompressionTests : AllowedWebSocketCompressionTests
{
    public CustomConfigurationCallbackWebSocketCompressionTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output) : base(browserFixture, serverFixture, output)
    {
        serverFixture.UpdateHostServices = services =>
        {
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            configuration.ConnectionDispatcherOptions = context => new() { DangerousEnableCompression = true };
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
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            configuration.IsCompressionEnabled = false;
            configuration.ConnectionDispatcherOptions = null;
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
        serverFixture.UpdateHostServices = services =>
        {
            var configuration = services.GetService<WebSocketCompressionConfiguration>();
            configuration.CspPolicy = "'none'";
        };
    }
}

