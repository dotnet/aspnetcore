// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using PlaywrightSharp;
using ProjectTemplates.Tests.Infrastructure;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BlazorServerTemplateTest
    {
        public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory, PlaywrightFixture<BlazorServerTemplateTest> fixture, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Fixture = fixture;
            Output = output;
            BrowserContextInfo = new ContextInformation(TestHelpers.CreateFactory(output));
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }

        public PlaywrightFixture<BlazorServerTemplateTest> Fixture { get; }

        public ITestOutputHelper Output { get; }
        public ContextInformation BrowserContextInfo { get; }
        public Project Project { get; private set; }


        [Theory]
        [InlineData(BrowserKind.Chromium)]
        public async Task BlazorServerTemplateWorks_NoAuth(BrowserKind browserKind)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            Project = await ProjectFactory.GetOrCreateProject("blazorservernoauth" + browserKind.ToString(), Output);

            var createResult = await Project.RunDotNetNewAsync("blazorserver");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                var page = await aspNetProcess.VisitInBrowserAsync(browser);
                await TestBasicNavigation(page);
                await page.CloseAsync();
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                var page = await aspNetProcess.VisitInBrowserAsync(browser);
                await TestBasicNavigation(page);
                await page.CloseAsync();
            }
        }

        [Theory]
        [InlineData(BrowserKind.Chromium, true)]
        [InlineData(BrowserKind.Chromium, false)]
        public async Task BlazorServerTemplateWorks_IndividualAuth(BrowserKind browserKind, bool useLocalDB)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            Project = await ProjectFactory.GetOrCreateProject("blazorserverindividual" + browserKind + (useLocalDB ? "uld" : ""), Output);

            var createResult = await Project.RunDotNetNewAsync("blazorserver", auth: "Individual", useLocalDB: useLocalDB);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (Fixture.BrowserManager.IsAvailable(browserKind))
                {
                    var page = await aspNetProcess.VisitInBrowserAsync(browser);
                    await TestBasicNavigation(page);
                    await page.CloseAsync();
                }
                else
                {
                    Assert.False(TestHelpers.TryValidateBrowserRequired(browserKind, isRequired: OperatingSystem.IsWindows(), out var error), error);
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
                    var page = await aspNetProcess.VisitInBrowserAsync(browser);
                    await TestBasicNavigation(page);
                    await page.CloseAsync();
                }
                else
                {
                    Assert.False(TestHelpers.TryValidateBrowserRequired(browserKind, isRequired: OperatingSystem.IsWindows(), out var error), error);
                }
            }
        }

        private async Task TestBasicNavigation(IPage page)
        {
            await page.QuerySelectorAsync("ul");
            // <title> element gets project ID injected into it during template execution
            Assert.Equal(Project.ProjectName.Trim(), (await page.GetTitleAsync()).Trim());

            // Initially displays the home page
            Assert.Equal("Hello, world!", await page.GetTextContentAsync("h1"));

            // Can navigate to the counter page

            await Task.WhenAll(
                page.WaitForNavigationAsync("**/counter"),
                page.WaitForSelectorAsync("text=Current count: 0"),
                page.ClickAsync("text=Counter"));

            Assert.Equal("Counter", await page.GetTextContentAsync("h1"));

            // Clicking the counter button works
            Assert.Equal("Current count: 0", await page.GetTextContentAsync("h1+p"));
            await page.WaitForTimeoutAsync(1000);
            await Task.WhenAll(
                page.WaitForSelectorAsync("text=Current count: 1"),
                page.ClickAsync("p+button"));

            Assert.Equal("Current count: 1", await page.GetTextContentAsync("h1+p"));

            // Can navigate to the 'fetch data' page
            await Task.WhenAll(
                page.WaitForNavigationAsync("**/fetchdata"),
                page.WaitForSelectorAsync("text=Weather forecast"),
                page.ClickAsync("text=Fetch data"));

            Assert.Equal("Weather forecast", await page.GetTextContentAsync("h1"));

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
        public async Task BlazorServerTemplat_IdentityWeb_BuildAndPublish(string auth, string[] args)
        {
            Project = await ProjectFactory.GetOrCreateProject("blazorserveridweb" + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), Output);

            var createResult = await Project.RunDotNetNewAsync("blazorserver", auth: auth, args: args);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));
        }
    }
}
