// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BlazorWasmTemplateTest : BrowserTestBase
    {
        public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory, BrowserFixture browserFixture, ITestOutputHelper output)
            : base(browserFixture, output)
        {
            ProjectFactory = projectFactory;
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/17681")]
        public async Task BlazorWasmStandaloneTemplate_Works()
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorstandalone", Output);
            project.TargetFramework = "netstandard2.1";

            var createResult = await project.RunDotNetNewAsync("blazorwasm");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            await BuildAndRunTest(project.ProjectName, project);

            var publishDir = Path.Combine(project.TemplatePublishDir, project.ProjectName, "dist");
            AspNetProcess.EnsureDevelopmentCertificates();

            Output.WriteLine("Running dotnet serve on published output...");
            using var serveProcess = ProcessEx.Run(Output, publishDir, DotNetMuxer.MuxerPathOrDefault(), "serve -S");

            // Todo: Use dynamic port assignment: https://github.com/natemcmaster/dotnet-serve/pull/40/files
            var listeningUri = "https://localhost:8080";
            Output.WriteLine($"Opening browser at {listeningUri}...");
            Browser.Navigate().GoToUrl(listeningUri);
            TestBasicNavigation(project.ProjectName);
        }

        [ConditionalFact]
        [SkipOnHelix("npm")]
        public async Task BlazorWasmHostedTemplate_Works()
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorhosted", Output);

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--hosted" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            var publishResult = await serverProject.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", serverProject, publishResult));

            var buildResult = await serverProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", serverProject, buildResult));

            await BuildAndRunTest(project.ProjectName, serverProject);

            using var aspNetProcess = serverProject.StartPublishedProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserFixture.IsHostAutomationSupported())
            {
                aspNetProcess.VisitInBrowser(Browser);
                TestBasicNavigation(project.ProjectName);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        protected async Task BuildAndRunTest(string appName, Project project)
        {
            using var aspNetProcess = project.StartBuiltProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserFixture.IsHostAutomationSupported())
            {
                aspNetProcess.VisitInBrowser(Browser);
                TestBasicNavigation(appName);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        private void TestBasicNavigation(string appName)
        {
            // Give components.server enough time to load so that it can replace
            // the prerendered content before we start making assertions.
            Thread.Sleep(5000);
            Browser.Exists(By.TagName("ul"));

            // <title> element gets project ID injected into it during template execution
            Browser.Equal(appName.Trim(), () => Browser.Title.Trim());

            // Initially displays the home page
            Browser.Equal("Hello, world!", () => Browser.FindElement(By.TagName("h1")).Text);

            // Can navigate to the counter page
            Browser.FindElement(By.PartialLinkText("Counter")).Click();
            Browser.Contains("counter", () => Browser.Url);
            Browser.Equal("Counter", () => Browser.FindElement(By.TagName("h1")).Text);

            // Clicking the counter button works
            Browser.Equal("Current count: 0", () => Browser.FindElement(By.CssSelector("h1 + p")).Text);
            Browser.FindElement(By.CssSelector("p+button")).Click();
            Browser.Equal("Current count: 1", () => Browser.FindElement(By.CssSelector("h1 + p")).Text);

            // Can navigate to the 'fetch data' page
            Browser.FindElement(By.PartialLinkText("Fetch data")).Click();
            Browser.Contains("fetchdata", () => Browser.Url);
            Browser.Equal("Weather forecast", () => Browser.FindElement(By.TagName("h1")).Text);

            // Asynchronously loads and displays the table of weather forecasts
            Browser.Exists(By.CssSelector("table>tbody>tr"));
            Browser.Equal(5, () => Browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
        }

        private Project GetSubProject(Project project, string projectDirectory, string projectName)
        {
            var subProjectDirectory = Path.Combine(project.TemplateOutputDir, projectDirectory);
            if (!Directory.Exists(subProjectDirectory))
            {
                throw new DirectoryNotFoundException($"Directory {subProjectDirectory} was not found.");
            }

            var subProject = new Project
            {
                DotNetNewLock = project.DotNetNewLock,
                NodeLock = project.NodeLock,
                Output = project.Output,
                DiagnosticsMessageSink = project.DiagnosticsMessageSink,
                ProjectName = projectName,
                TemplateOutputDir = subProjectDirectory,
            };

            return subProject;
        }
    }
}
