// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class StreamedBrowserConfigurationTest(
    BrowserFixture browserFixture,
    BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{
    public override Task InitializeAsync()
        => base.InitializeAsync(BrowserFixture.StreamingContext);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializersReceiveBrowserConfigurationWithStreamingRendering(bool initialBoundary)
    {
        var operationId = Guid.NewGuid().ToString("N");
        using var client = new HttpClient { BaseAddress = _serverFixture.RootUri };
        var releaseUrl = $"{ServerPathBase}/browser-configuration-streaming/{operationId}";

        try
        {
            Navigate($"{ServerPathBase}/browser-configuration-streaming?OperationId={operationId}&InitialBoundary={initialBoundary}");
            Browser.Equal("Waiting", () => Browser.Exists(By.Id("browser-configuration-streaming-status")).Text);
            Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
                "return customElements.get('blazor-ssr-end') !== undefined"));
            Assert.Equal("loading", ((IJavaScriptExecutor)Browser).ExecuteScript("return document.readyState"));
            Assert.Equal(initialBoundary, HasComment("Blazor:"));
            Assert.Equal(initialBoundary, HasComment("Blazor-Configuration:"));
            Browser.DoesNotExist(By.Id("browser-configuration-options"));

            using (var response = await client.PostAsync($"{releaseUrl}/false", null))
            {
                response.EnsureSuccessStatusCode();
            }

            Browser.Equal("Boundary delivered", () => Browser.Exists(By.Id("browser-configuration-streaming-status")).Text);
            Assert.Equal("loading", ((IJavaScriptExecutor)Browser).ExecuteScript("return document.readyState"));
            Browser.DoesNotExist(By.Id("browser-configuration-options"));

            using (var response = await client.PostAsync($"{releaseUrl}/true", null))
            {
                response.EnsureSuccessStatusCode();
            }

            Browser.Equal(
                """{"logLevel":1,"dialogId":"streamed-configuration-dialog","maxRetries":17}""",
                () => Browser.Exists(By.Id("browser-configuration-options")).Text);
            Browser.Equal("True", () => Browser.Exists(By.Id("is-interactive-browser-configuration")).Text);
            Browser.Click(By.Id("increment-browser-configuration"));
            Browser.Equal("1", () => Browser.Exists(By.Id("count-browser-configuration")).Text);
        }
        finally
        {
            using var response = await client.PostAsync($"{releaseUrl}/true", null);
            Assert.True(response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.NotFound);
        }
    }

    private bool HasComment(string prefix)
        => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            """
            const walker = document.createTreeWalker(document, NodeFilter.SHOW_COMMENT);
            while (walker.nextNode()) {
                if (walker.currentNode.textContent.startsWith(arguments[0])) {
                    return true;
                }
            }
            return false;
            """, prefix);
}
