// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class RenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public RenderingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanRenderLargeComponentsWithServerRenderMode()
    {
        Navigate($"{ServerPathBase}/large-html-server");
        var result = new string('*', 50000);

        Browser.Equal(result, () => Browser.FindElement(By.Id("webassembly-prerender")).Text);
        Browser.Equal(result, () => Browser.FindElement(By.Id("server-no-prerender")).Text);
        Browser.Equal(result, () => Browser.FindElement(By.Id("server-prerender")).Text);
    }

    [Fact]
    public async Task CanUseHttpContextRequestAndResponse()
    {
        Navigate($"{ServerPathBase}/httpcontext");
        Browser.Equal("GET", () => Browser.FindElement(By.Id("request-method")).Text);
        Browser.Equal("/httpcontext", () => Browser.FindElement(By.Id("request-path")).Text);

        // We can't see the response status code using Selenium, so make a direct request
        var response = await new HttpClient().GetAsync(Browser.Url);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public void RendersEndStateOfComponentsOnSSRPage()
    {
        Navigate($"{ServerPathBase}/ssr-page-that-delays-loading");
        Browser.Equal("loaded child", () => Browser.Exists(By.Id("child")).Text);
    }

    [Fact]
    public void PostRequestRendersEndStateOfComponentsOnSSRPage()
    {
        Navigate($"{ServerPathBase}/forms/post-form-with-component-that-delays-loading");

        Browser.Exists(By.Id("submit-button")).Click();

        Browser.Equal("loaded child", () => Browser.Exists(By.Id("child")).Text);
    }

    [Theory]
    [InlineData(false, "ServerPrerendered", true)]
    [InlineData(false, "ServerPrerendered", false)]
    [InlineData(true, "ServerPrerendered", false)]
    [InlineData(true, "ServerNonPrerendered", false)]
    [InlineData(true, "WebAssemblyPrerendered", false)]
    [InlineData(true, "WebAssemblyNonPrerendered", false)]
    public async Task RenderBatchQueuedAfterRedirectionIsNotProcessed(bool redirect, string renderMode, bool throwSync)
    {
        string relativeUri = $"subdir/stopping-renderer?renderMode={renderMode}";
        if (redirect)
        {
            relativeUri += $"&destination=redirect";
        }

        // async operation forces the next render batch
        if (throwSync)
        {
            relativeUri += $"&delay=0";
        }
        else
        {
            relativeUri += $"&delay=1";
        }

        var requestUri = new Uri(_serverFixture.RootUri, relativeUri);
        var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var response = await httpClient.GetAsync(requestUri);

        if (redirect)
        {
            var expectedUri = new Uri(_serverFixture.RootUri, "subdir/redirect");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(expectedUri, response.Headers.Location);
        }
        else
        {
            // the status code cannot be changed after it got set, so async throwing returns OK
            if (throwSync)
            {
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
