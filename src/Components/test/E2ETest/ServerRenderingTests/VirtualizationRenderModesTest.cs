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

public class VirtualizationRenderModesTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public VirtualizationRenderModesTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void Virtualize_Works_WhenMultipleRenderModesAreActive()
    {
        Navigate($"{ServerPathBase}/interactivity/virtualization");

        Browser.Equal("interactive", () => Browser.FindElement(By.Id("virtualize-server")).GetAttribute("class"));
        Browser.Equal("interactive", () => Browser.FindElement(By.Id("virtualize-webassembly")).GetAttribute("class"));

        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 1"));
        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 1"));
        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 50"));
        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 50"));

        ScrollTopToEnd(Browser, Browser.FindElement(By.Id("virtualize-server")));
        ScrollTopToEnd(Browser, Browser.FindElement(By.Id("virtualize-webassembly")));

        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 1"));
        Browser.False(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 1"));
        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-server"))).Contains("Item 50"));
        Browser.True(() => GetRenderedItems(Browser.FindElement(By.Id("virtualize-webassembly"))).Contains("Item 50"));
    }

    private static string[] GetRenderedItems(IWebElement container)
    {
        var itemElements = container.FindElements(By.CssSelector(".virtualize-item"));
        return itemElements.Select(element => element.Text).ToArray();
    }

    private static void ScrollTopToEnd(IWebDriver browser, IWebElement elem)
    {
        var js = (IJavaScriptExecutor)browser;
        js.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight", elem);
    }
}
