// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class BlazorWebJsInitializersTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public BlazorWebJsInitializersTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
    {
        return base.InitializeAsync(BrowserFixture.StreamingContext);
    }

    [Theory]
    [MemberData(nameof(InitializerTestData))]
    public void InitializersRunsModernCallbacksByDefaultWhenPresent(bool streaming, bool webassembly, bool server, string[] expectedInvokedCallbacks)
    {
        var url = $"{ServerPathBase}/initializers?streaming={streaming}&wasm={webassembly}&server={server}";
        Navigate(url);

        if (webassembly)
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('block-webassembly-settings', 'true')");
        }

        foreach (var callback in expectedInvokedCallbacks)
        {
            Browser.Exists(By.Id(callback));
        }

        Browser.Equal(expectedInvokedCallbacks.Length, () => Browser.FindElements(By.CssSelector("#initializers-content > p")).Count);

        if (server)
        {
            Browser.Click(By.Id("remove-server-component"));
            Browser.Exists(By.Id("classic-and-modern-circuit-closed"));
        }
    }

    [Theory]
    [MemberData(nameof(InitializerTestData))]
    public void InitializersRunsClassicInitializersWhenEnabled(bool streaming, bool webassembly, bool server, string[] expectedInvokedCallbacks)
    {
        EnableClassicInitializers(Browser);
        List<string> expectedCallbacks = ["classic-before-start", "classic-after-started", ..expectedInvokedCallbacks];
        var url = $"{ServerPathBase}/initializers?streaming={streaming}&wasm={webassembly}&server={server}";
        Navigate(url);

        if (webassembly)
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('block-webassembly-settings', 'true')");
        }

        foreach (var callback in expectedCallbacks)
        {
            Browser.Exists(By.Id(callback));
        }

        Browser.Equal(expectedCallbacks.Count, () => Browser.FindElements(By.CssSelector("#initializers-content > p")).Count);

        if (server)
        {
            Browser.Click(By.Id("remove-server-component"));
            Browser.Exists(By.Id("classic-and-modern-circuit-closed"));
        }
    }

    private void EnableClassicInitializers(IWebDriver browser)
    {
        browser.Navigate().GoToUrl($"{new Uri(_serverFixture.RootUri, ServerPathBase)}/");
        ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.setItem('enable-classic-initializers', 'true')");
    }

    public static TheoryData<bool, bool, bool, string[]> InitializerTestData()
    {
        var result = new TheoryData<bool, bool, bool, string[]>
        {
            { false, false, false, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started","modern-before-web-start", "modern-after-web-started"] },
            { false, false, true, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started", "classic-and-modern-before-server-start", "classic-and-modern-after-server-started", "classic-and-modern-circuit-opened", "modern-before-web-start", "modern-after-web-started", "modern-before-server-start", "modern-after-server-started", "modern-circuit-opened" ] },
            { false, true, false, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started", "classic-and-modern-before-web-assembly-start", "classic-and-modern-after-web-assembly-started", "server--classic-and-modern-before-web-assembly-start", "server--classic-and-modern-after-web-assembly-started", "modern-before-web-start", "modern-after-web-started", "modern-before-web-assembly-start", "modern-after-web-assembly-started"] },
            { false, true, true, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started", "classic-and-modern-before-server-start", "classic-and-modern-circuit-opened", "classic-and-modern-after-server-started", "classic-and-modern-before-web-assembly-start", "classic-and-modern-after-web-assembly-started", "server--classic-and-modern-before-web-assembly-start", "server--classic-and-modern-after-web-assembly-started", "modern-before-web-start", "modern-after-web-started", "modern-before-server-start", "modern-circuit-opened", "modern-after-server-started", "modern-before-web-assembly-start", "modern-after-web-assembly-started"] },
            { true, false, false, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started","modern-before-web-start", "modern-after-web-started"] },
            { true, false, true, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started", "classic-and-modern-before-server-start", "classic-and-modern-after-server-started", "classic-and-modern-circuit-opened", "modern-before-web-start", "modern-after-web-started", "modern-before-server-start", "modern-after-server-started", "modern-circuit-opened" ] },
            { true, true, false, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started", "classic-and-modern-before-web-assembly-start", "classic-and-modern-after-web-assembly-started", "server--classic-and-modern-before-web-assembly-start", "server--classic-and-modern-after-web-assembly-started", "modern-before-web-start", "modern-after-web-started", "modern-before-web-assembly-start", "modern-after-web-assembly-started"] },
            { true, true, true, ["classic-and-modern-before-web-start", "classic-and-modern-after-web-started", "classic-and-modern-before-server-start", "classic-and-modern-circuit-opened", "classic-and-modern-after-server-started", "classic-and-modern-before-web-assembly-start", "classic-and-modern-after-web-assembly-started", "server--classic-and-modern-before-web-assembly-start", "server--classic-and-modern-after-web-assembly-started", "modern-before-web-start", "modern-after-web-started", "modern-before-server-start", "modern-circuit-opened", "modern-after-server-started", "modern-before-web-assembly-start", "modern-after-web-assembly-started"] },
        };

        return result;
    }
}
