// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BlazorServerTemplateTest : BrowserTestBase
    {
        public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory, BrowserFixture browserFixture, ITestOutputHelper output) : base(browserFixture, output)
        {
            ProjectFactory = projectFactory;
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }

        public Project Project { get; private set; }

        [ConditionalFact]
        [SkipOnHelix("selenium")]
        public async Task BlazorServerTemplateWorks_NoAuth()
        {
            Project = await ProjectFactory.GetOrCreateProject("blazorservernoauth", Output);

            var createResult = await Project.RunDotNetNewAsync("blazorserver");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (BrowserFixture.IsHostAutomationSupported())
                {
                    aspNetProcess.VisitInBrowser(Browser);
                    TestBasicNavigation();
                }
                else
                {
                    BrowserFixture.EnforceSupportedConfigurations();
                }
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (BrowserFixture.IsHostAutomationSupported())
                {
                    aspNetProcess.VisitInBrowser(Browser);
                    TestBasicNavigation();
                }
                else
                {
                    BrowserFixture.EnforceSupportedConfigurations();
                }
            }
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnHelix("ef restore no worky")]
        public async Task BlazorServerTemplateWorks_IndividualAuth(bool useLocalDB)
        {
            Project = await ProjectFactory.GetOrCreateProject("blazorserverindividual" + (useLocalDB ? "uld" : ""), Output);

            var createResult = await Project.RunDotNetNewAsync("blazorserver", auth: "Individual", useLocalDB: useLocalDB);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (BrowserFixture.IsHostAutomationSupported())
                {
                    aspNetProcess.VisitInBrowser(Browser);
                    TestBasicNavigation();
                }
                else
                {
                    BrowserFixture.EnforceSupportedConfigurations();
                }
            }


            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                if (BrowserFixture.IsHostAutomationSupported())
                {
                    aspNetProcess.VisitInBrowser(Browser);
                    TestBasicNavigation();
                }
            }
        }

        private void TestBasicNavigation()
        {
            // Give components.server enough time to load so that it can replace
            // the prerendered content before we start making assertions.
            Thread.Sleep(5000);
            Browser.Exists(By.TagName("ul"));
            // <title> element gets project ID injected into it during template execution
            Browser.Equal(Project.ProjectName.Trim(), () => Browser.Title.Trim());

            // Initially displays the home page
            Browser.Equal("Hello, world!", () => Browser.FindElement(By.TagName("h1")).Text);

            // Can navigate to the counter page
            Browser.Click(By.PartialLinkText("Counter"));
            Browser.Contains("counter", () => Browser.Url);
            Browser.Equal("Counter", () => Browser.FindElement(By.TagName("h1")).Text);

            // Clicking the counter button works
            Browser.Equal("Current count: 0", () => Browser.FindElement(By.CssSelector("h1 + p")).Text);
            Browser.Click(By.CssSelector("p+button"));
            Browser.Equal("Current count: 1", () => Browser.FindElement(By.CssSelector("h1 + p")).Text);

            // Can navigate to the 'fetch data' page
            Browser.Click(By.PartialLinkText("Fetch data"));
            Browser.Contains("fetchdata", () => Browser.Url);
            Browser.Equal("Weather forecast", () => Browser.FindElement(By.TagName("h1")).Text);

            // Asynchronously loads and displays the table of weather forecasts
            Browser.Exists(By.CssSelector("table>tbody>tr"));
            Browser.Equal(5, () => Browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
        }
    }
}
