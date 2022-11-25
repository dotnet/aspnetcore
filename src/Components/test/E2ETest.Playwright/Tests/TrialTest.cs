// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Playwright.Tests;

public class TrialTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public TrialTest(BasicTestAppServerSiteFixture<ServerStartup> serverFixture, ITestOutputHelper output)
        : base(serverFixture, output)
    {
    }

    [Fact]
    public async Task SomeTest()
    {
        await using var browser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
        var page = await NavigateToPage(browser, new Uri(_serverFixture.RootUri, ServerPathBase).ToString());
        await page.WaitForSelectorAsync("body");
    }

    private static async Task<IPage> NavigateToPage(IBrowserContext browser, string listeningUri)
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(listeningUri, new() { WaitUntil = WaitUntilState.NetworkIdle });
        return page;
    }
}
