// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Playwright;
using ProjectTemplates.Tests.Infrastructure;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace BlazorTemplates.Tests;

public class BlazorServerTemplateTest : BlazorTemplateTest
{
    public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory)
    {
    }

    public override string ProjectType { get; } = "blazorserver";

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorServerTemplateWorks_NoAuth(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync();

        await using var browser = BrowserManager.IsAvailable(browserKind) ?
            await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo) :
            null;

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

            if (BrowserManager.IsAvailable(browserKind))
            {
                var page = await browser.NewPageAsync();
                await aspNetProcess.VisitInBrowserAsync(page);
                await TestBasicNavigation(page);
                await page.CloseAsync();
            }
            else
            {
                EnsureBrowserAvailable(browserKind);
            }
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserManager.IsAvailable(browserKind))
            {
                var page = await browser.NewPageAsync();
                await aspNetProcess.VisitInBrowserAsync(page);
                await TestBasicNavigation(page);
                await page.CloseAsync();
            }
            else
            {
                EnsureBrowserAvailable(browserKind);
            }
        }
    }

    [InlineData(BrowserKind.Chromium)]
    [Theory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
    public async Task BlazorServerTemplateWorks_IndividualAuth(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync();

        var browser = !BrowserManager.IsAvailable(browserKind) ?
            null :
            await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserManager.IsAvailable(browserKind))
            {
                var page = await browser.NewPageAsync();
                await aspNetProcess.VisitInBrowserAsync(page);
                await TestBasicNavigation(page);
                await page.CloseAsync();
            }
            else
            {
                EnsureBrowserAvailable(browserKind);
            }
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserManager.IsAvailable(browserKind))
            {
                var page = await browser.NewPageAsync();
                await aspNetProcess.VisitInBrowserAsync(page);
                await TestBasicNavigation(page);
                await page.CloseAsync();
            }
            else
            {
                EnsureBrowserAvailable(browserKind);
            }
        }
    }

    private async Task TestBasicNavigation(IPage page)
    {
        // Wait for the page to load, and the connection to idle for >500ms
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60_000 });

        // <title> element gets project ID injected into it during template execution
        Assert.Equal("Index", (await page.TitleAsync()).Trim());

        // Initially displays the home page
        await page.WaitForSelectorAsync("h1 >> text=Hello, world!");

        // Can navigate to the counter page
        await page.ClickAsync("a[href=counter] >> text=Counter");
        await page.WaitForSelectorAsync("h1+p >> text=Current count: 0");

        // Clicking the counter button works
        for (var i = 1; i <= 3; i++)
        {
            await page.ClickAsync("p+button >> text=Click me");
            await page.WaitForSelectorAsync($"h1+p >> text=Current count: {i}");
        }

        // Can navigate to the 'Fetch Data' page
        await page.ClickAsync("a[href=fetchdata] >> text=Fetch data");
        await page.WaitForSelectorAsync("h1 >> text=Weather forecast");

        // Asynchronously loads and displays the table of weather forecasts
        await page.WaitForSelectorAsync("table>tbody>tr");
        Assert.Equal(5, await page.Locator("p+table>tbody>tr").CountAsync());
    }

    [Theory(Skip="https://github.com/dotnet/aspnetcore/issues/46430")]
    [InlineData("IndividualB2C", null)]
    [InlineData("IndividualB2C", new [] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new [] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
    [InlineData("SingleOrg", new [] { "--calls-graph" })]
    public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish(string auth, string[] args)
        => CreateBuildPublishAsync(auth, args);
}
