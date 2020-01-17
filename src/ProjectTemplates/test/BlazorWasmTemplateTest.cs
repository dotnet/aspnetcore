// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.CommandLineUtils;
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

        [Fact]
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

        [Fact]
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BlazorWasmHostedTemplate_IndividualAuth_Works(bool useLocalDb)
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorhostedindividual" + (useLocalDb ? "uld" : ""), Output);

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--hosted", "-au", "Individual", useLocalDb ? "-uld" : "" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            var serverProjectFileContents = ReadFile(serverProject.TemplateOutputDir, $"{serverProject.ProjectName}.csproj");
            if (!useLocalDb)
            {
                Assert.Contains(".db", serverProjectFileContents);
            }
            var publishResult = await serverProject.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", serverProject, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await serverProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", serverProject, buildResult));

            var migrationsResult = await serverProject.RunDotNetEfCreateMigrationAsync("blazorwasm");
            Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", serverProject, migrationsResult));
            serverProject.AssertEmptyMigration("blazorwasm");

            if (useLocalDb)
            {
                using var dbUpdateResult = await serverProject.RunDotNetEfUpdateDatabaseAsync();
                Assert.True(0 == dbUpdateResult.ExitCode, ErrorMessages.GetFailedProcessMessage("update database", serverProject, dbUpdateResult));
            }

            await BuildAndRunTest(project.ProjectName, serverProject, usesAuth: true);

            using var aspNetProcess = serverProject.StartPublishedProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserFixture.IsHostAutomationSupported())
            {
                aspNetProcess.VisitInBrowser(Browser);
                TestBasicNavigation(project.ProjectName, usesAuth: true);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        protected async Task BuildAndRunTest(string appName, Project project, bool usesAuth = false)
        {
            using var aspNetProcess = project.StartBuiltProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (BrowserFixture.IsHostAutomationSupported())
            {
                aspNetProcess.VisitInBrowser(Browser);
                TestBasicNavigation(appName, usesAuth);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        private void TestBasicNavigation(string appName, bool usesAuth = false)
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

            if (usesAuth)
            {
                Browser.FindElement(By.PartialLinkText("Log in")).Click();
                Browser.Contains("/Identity/Account/Login", () => Browser.Url);

                Browser.FindElement(By.PartialLinkText("Register as a new user")).Click();

                var userName = $"{Guid.NewGuid()}@example.com";
                var password = $"!Test.Password1$";
                Browser.Exists(By.Name("Input.Email"));
                Browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
                Browser.FindElement(By.Name("Input.Password")).SendKeys(password);
                Browser.FindElement(By.Name("Input.ConfirmPassword")).SendKeys(password);
                Browser.FindElement(By.Id("registerSubmit")).Click();

                // We will be redirected to the RegisterConfirmation
                Browser.Contains("/Identity/Account/RegisterConfirmation", () => Browser.Url);
                Browser.FindElement(By.PartialLinkText("Click here to confirm your account")).Click();

                // We will be redirected to the ConfirmEmail
                Browser.Contains("/Identity/Account/ConfirmEmail", () => Browser.Url);

                // Now we can login
                Browser.FindElement(By.PartialLinkText("Login")).Click();
                Browser.Exists(By.Name("Input.Email"));
                Browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
                Browser.FindElement(By.Name("Input.Password")).SendKeys(password);
                Browser.FindElement(By.Id("login-submit")).Click();

                // Need to navigate to fetch page
                Browser.Navigate().GoToUrl(new Uri(Browser.Url).GetLeftPart(UriPartial.Authority));
                Browser.Equal(appName.Trim(), () => Browser.Title.Trim());
            }

            // Can navigate to the 'fetch data' page
            Browser.FindElement(By.PartialLinkText("Fetch data")).Click();
            Browser.Contains("fetchdata", () => Browser.Url);
            Browser.Equal("Weather forecast", () => Browser.FindElement(By.TagName("h1")).Text);

            // Asynchronously loads and displays the table of weather forecasts
            Browser.Exists(By.CssSelector("table>tbody>tr"));
            Browser.Equal(5, () => Browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
        }

        private string ReadFile(string basePath, string path)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
            return File.ReadAllText(Path.Combine(basePath, path));
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
