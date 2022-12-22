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

    [Theory(Skip = "https://github.com/dotnet/aspnetcore/issues/30761")]
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
    [Theory(Skip = "https://github.com/dotnet/aspnetcore/issues/30882")]
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
        var socket = await page.WaitForWebSocketAsync();

        var framesReceived = 0;
        var framesSent = 0;

        // We wait for the first two frames
        // Receive render batch
        // JS interop call to intercept navigation
        var twoFramesReceived = new TaskCompletionSource();
        var twoFramesSent = new TaskCompletionSource();

        void FrameReceived(object sender, IWebSocketFrame frame)
        {
            framesReceived++;
            if (framesReceived == 2)
            {
                twoFramesReceived.SetResult();
            }
        }
        void FrameSent(object sender, IWebSocketFrame frame)
        {
            framesSent++;
            if (framesSent == 2)
            {
                twoFramesSent.SetResult();
            }
        }

        socket.FrameReceived += FrameReceived;
        socket.FrameSent += FrameSent;

        await twoFramesReceived.Task;
        await twoFramesSent.Task;

        socket.FrameReceived -= FrameReceived;
        socket.FrameSent -= FrameSent;

        await page.WaitForSelectorAsync("nav");
        // <title> element gets project ID injected into it during template execution
        Assert.Equal("Index", (await page.TitleAsync()).Trim());

        // Initially displays the home page
        await page.WaitForSelectorAsync("h1 >> text=Hello, world!");

        // Can navigate to the counter page
        await page.ClickAsync("a[href=counter] >> text=Counter");
        await page.WaitForSelectorAsync("h1+p >> text=Current count: 0");

        // Clicking the counter button works
        await page.ClickAsync("p+button >> text=Click me");
        await page.WaitForSelectorAsync("h1+p >> text=Current count: 1");

        // Can navigate to the 'fetch data' page
        await page.ClickAsync("a[href=fetchdata] >> text=Fetch data");
        await page.WaitForSelectorAsync("h1 >> text=Weather forecast");

        // Asynchronously loads and displays the table of weather forecasts
        await page.WaitForSelectorAsync("table>tbody>tr");
        Assert.Equal(5, await page.Locator("p+table>tbody>tr").CountAsync());
    }

    [Theory]
    [InlineData("IndividualB2C", null)]
    [InlineData("IndividualB2C", new [] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new [] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
    [InlineData("SingleOrg", new [] { "--calls-graph" })]
    public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish(string auth, string[] args)
        => CreateBuildPublishAsync(auth, args);
}
