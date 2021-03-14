// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    [TestCaseOrderer("Templates.Test.PriorityOrderer", "BlazorTemplates.Tests")]
    public class BlazorServerTemplateTest : BlazorTemplateTest
    {
        public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory, PlaywrightFixture<BlazorServerTemplateTest> fixture, ITestOutputHelper output)
            : base(projectFactory, fixture, output)
        {
        }

        public override string ProjectType { get; } = "blazorserver";

        [Fact, TestPriority(BUILDCREATEPUBLISH_PRIORITY)]
        public Task BlazorServerTemplate_CreateBuildPublish_NoAuth()
            => CreateBuildPublishAsync("blazorservernoauth" + BrowserKind.Chromium.ToString());

        [Theory, TestPriority(BUILDCREATEPUBLISH_PRIORITY)]
        [MemberData(nameof(BlazorServerTemplateWorks_IndividualAuthData))]
        public Task BlazorServerTemplate_CreateBuildPublish_IndividualAuthUseLocalDb(BrowserKind browserKind, bool useLocalDB)
            => CreateBuildPublishAsync("blazorserverindividual" + browserKind + (useLocalDB ? "uld" : ""));

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        //[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30761")]
        public async Task BlazorServerTemplateWorks_NoAuth(BrowserKind browserKind)
        {
            Project = await ProjectFactory.GetOrCreateProject("blazorservernoauth" + browserKind.ToString(), Output);

            await using var browser = Fixture.BrowserManager.IsAvailable(browserKind) ?
                await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo) :
                null;

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                if (Fixture.BrowserManager.IsAvailable(browserKind))
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

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (Fixture.BrowserManager.IsAvailable(browserKind))
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

        public static IEnumerable<object[]> BlazorServerTemplateWorks_IndividualAuthData =>
                BrowserManager.WithBrowsers(new[] { BrowserKind.Chromium }, true, false);

        [Theory]
        [MemberData(nameof(BlazorServerTemplateWorks_IndividualAuthData))]
        //[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30807")]
        public async Task BlazorServerTemplateWorks_IndividualAuth(BrowserKind browserKind, bool useLocalDB)
        {
            Project = await ProjectFactory.GetOrCreateProject("blazorserverindividual" + browserKind + (useLocalDB ? "uld" : ""), Output);

            var browser = !Fixture.BrowserManager.IsAvailable(browserKind) ?
                null :
                await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (Fixture.BrowserManager.IsAvailable(browserKind))
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

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (Fixture.BrowserManager.IsAvailable(browserKind))
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
            var socket = BrowserContextInfo.Pages[page].WebSockets.SingleOrDefault() ??
                (await page.WaitForEventAsync(PageEvent.WebSocket)).WebSocket;

            // Receive render batch
            await socket.WaitForEventAsync(WebSocketEvent.FrameReceived);
            await socket.WaitForEventAsync(WebSocketEvent.FrameSent);

            // JS interop call to intercept navigation
            await socket.WaitForEventAsync(WebSocketEvent.FrameReceived);
            await socket.WaitForEventAsync(WebSocketEvent.FrameSent);

            await page.WaitForSelectorAsync("ul");
            // <title> element gets project ID injected into it during template execution
            Assert.Equal(Project.ProjectName.Trim(), (await page.GetTitleAsync()).Trim());

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

        [Theory, TestPriority(BUILDCREATEPUBLISH_PRIORITY)]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new string[] { "--called-api-url \"https://graph.microsoft.com\"", "--called-api-scopes user.readwrite" })]
        [InlineData("SingleOrg", new string[] { "--calls-graph" })]
        //[QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30882")]
        public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish(string auth, string[] args)
            => CreateBuildPublishAsync("blazorserveridweb" + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), auth, args);

    }
}
