// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using PlaywrightSharp;
using ProjectTemplates.Tests.Infrastructure;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    [Retry]
    public class BlazorServerTemplateTest : BlazorTemplateTest
    {
        public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory)
            : base(projectFactory)
        {
        }

        public override string ProjectType { get; } = "blazorserver";

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30761")]
        public async Task BlazorServerTemplateWorks_NoAuth(BrowserKind browserKind)
        {
            var project = await CreateBuildPublishAsync("blazorservernoauth" + browserKind);

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
                    await TestBasicNavigation(project, page);
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
                    await TestBasicNavigation(project, page);
                    await page.CloseAsync();
                }
                else
                {
                    EnsureBrowserAvailable(browserKind);
                }
            }
        }

        public static IEnumerable<object[]> BlazorServerTemplateWorks_IndividualAuthData =>
                BrowserManager.WithBrowsers(new[] { BrowserKind.Chromium }, true, false);

        [Theory]
        [MemberData(nameof(BlazorServerTemplateWorks_IndividualAuthData))]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30882")]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
        public async Task BlazorServerTemplateWorks_IndividualAuth(BrowserKind browserKind, bool useLocalDB)
        {
            var project = await CreateBuildPublishAsync("blazorserverindividual" + browserKind + (useLocalDB ? "uld" : ""));

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
                    await TestBasicNavigation(project, page);
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
                    await TestBasicNavigation(project, page);
                    await page.CloseAsync();
                }
                else
                {
                    EnsureBrowserAvailable(browserKind);
                }
            }
        }

        private async Task TestBasicNavigation(Project project, IPage page)
        {
            var socket = BrowserContextInfo.Pages[page].WebSockets.SingleOrDefault() ??
                (await page.WaitForEventAsync(PageEvent.WebSocket)).WebSocket;

            // Receive render batch
            await socket.WaitForEventAsync(WebSocketEvent.FrameReceived);
            await socket.WaitForEventAsync(WebSocketEvent.FrameSent);

            // JS interop call to intercept navigation
            await socket.WaitForEventAsync(WebSocketEvent.FrameReceived);
            await socket.WaitForEventAsync(WebSocketEvent.FrameSent);

            await page.WaitForSelectorAsync("nav");
            // <title> element gets project ID injected into it during template execution
            Assert.Equal("Index", (await page.GetTitleAsync()).Trim());

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
            Assert.Equal(5, (await page.QuerySelectorAllAsync("p+table>tbody>tr")).Count());
        }

        [Theory]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", new string[] { "--calls-graph" })]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30882")]
        public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish(string auth, string[] args)
            => CreateBuildPublishAsync("blazorserveridweb" + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), auth, args);

    }
}
