// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
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

        public override Task InitializeAsync()
        {
            return InitializeAsync(isolationContext: Guid.NewGuid().ToString());
        }

        [Fact]
        public async Task BlazorWasmStandaloneTemplate_Works()
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorstandalone", Output);
            project.RuntimeIdentifier = "browser-wasm";

            var createResult = await project.RunDotNetNewAsync("blazorwasm");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // The service worker assets manifest isn't generated for non-PWA projects
            var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
            Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            await BuildAndRunTest(project.ProjectName, project);

            if (BrowserFixture.IsHostAutomationSupported())
            {
                var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
                using (serveProcess)
                {
                    Output.WriteLine($"Opening browser at {listeningUri}...");
                    Browser.Navigate().GoToUrl(listeningUri);
                    TestBasicNavigation(project.ProjectName);
                }
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/20172")]
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
            await AssertCompressionFormat(aspNetProcess, "br");
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

        private static async Task AssertCompressionFormat(AspNetProcess aspNetProcess, string expectedEncoding)
        {
            var response = await aspNetProcess.SendRequest(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(aspNetProcess.ListeningUri, "/_framework/blazor.boot.json"));
                // These are the same as chrome
                request.Headers.AcceptEncoding.Clear();
                request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
                request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));
                request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("br"));

                return request;
            });
            Assert.Equal(expectedEncoding, response.Content.Headers.ContentEncoding.Single());
        }

        [Fact]
        public async Task BlazorWasmStandalonePwaTemplate_Works()
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorstandalonepwa", Output);
            project.RuntimeIdentifier = "browser-wasm";

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--pwa" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            await BuildAndRunTest(project.ProjectName, project);

            ValidatePublishedServiceWorker(project);

            if (BrowserFixture.IsHostAutomationSupported())
            {
                var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
                using (serveProcess)
                {
                    Output.WriteLine($"Opening browser at {listeningUri}...");
                    Browser.Navigate().GoToUrl(listeningUri);
                    TestBasicNavigation(project.ProjectName);
                }

                // The PWA template supports offline use. By now, the browser should have cached everything it needs,
                // so we can continue working even without the server.
                ValidateAppWorksOffline(project, listeningUri, skipFetchData: false);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        [Fact]
        public async Task BlazorWasmHostedPwaTemplate_Works()
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorhostedpwa", Output);

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--hosted", "--pwa" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            var publishResult = await serverProject.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", serverProject, publishResult));

            var buildResult = await serverProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", serverProject, buildResult));

            await BuildAndRunTest(project.ProjectName, serverProject);

            ValidatePublishedServiceWorker(serverProject);

            string listeningUri = null;
            if (BrowserFixture.IsHostAutomationSupported())
            {
                using (var aspNetProcess = serverProject.StartPublishedProjectAsync())
                {
                    Assert.False(
                        aspNetProcess.Process.HasExited,
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

                    await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                    aspNetProcess.VisitInBrowser(Browser);
                    TestBasicNavigation(project.ProjectName);

                    // Note: we don't want to use aspNetProcess.ListeningUri because that isn't necessarily the HTTPS URI
                    var browserUri = new Uri(Browser.Url);
                    listeningUri = $"{browserUri.Scheme}://{browserUri.Authority}";
                }

                // The PWA template supports offline use. By now, the browser should have cached everything it needs,
                // so we can continue working even without the server.
                // Since this is the hosted project, backend APIs won't work offline, so we need to skip "fetchdata"
                ValidateAppWorksOffline(project, listeningUri, skipFetchData: true);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        private void ValidatePublishedServiceWorker(Project project)
        {
            var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

            // When publishing the PWA template, we generate an assets manifest
            // and move service-worker.published.js to overwrite service-worker.js
            Assert.False(File.Exists(Path.Combine(publishDir, "service-worker.published.js")), "service-worker.published.js should not be published");
            Assert.True(File.Exists(Path.Combine(publishDir, "service-worker.js")), "service-worker.js should be published");
            Assert.True(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "service-worker-assets.js should be published");

            // We automatically append the SWAM version as a comment in the published service worker file
            var serviceWorkerAssetsManifestContents = ReadFile(publishDir, "service-worker-assets.js");
            var serviceWorkerContents = ReadFile(publishDir, "service-worker.js");

            // Parse the "version": "..." value from the SWAM, and check it's in the service worker
            var serviceWorkerAssetsManifestVersionMatch = new Regex(@"^\s*\""version\"":\s*(\""[^\""]+\"")", RegexOptions.Multiline)
                .Match(serviceWorkerAssetsManifestContents);
            Assert.True(serviceWorkerAssetsManifestVersionMatch.Success);
            var serviceWorkerAssetsManifestVersionJson = serviceWorkerAssetsManifestVersionMatch.Groups[1].Captures[0].Value;
            var serviceWorkerAssetsManifestVersion = JsonSerializer.Deserialize<string>(serviceWorkerAssetsManifestVersionJson);
            Assert.True(serviceWorkerContents.Contains($"/* Manifest version: {serviceWorkerAssetsManifestVersion} */", StringComparison.Ordinal));
        }

        private void ValidateAppWorksOffline(Project project, string listeningUri, bool skipFetchData)
        {
            Browser.Navigate().GoToUrl("about:blank"); // Be sure we're really reloading
            Output.WriteLine($"Opening browser without corresponding server at {listeningUri}...");
            Browser.Navigate().GoToUrl(listeningUri);
            TestBasicNavigation(project.ProjectName, skipFetchData: skipFetchData);
        }

        [ConditionalFact]
        // LocalDB doesn't work on non Windows platforms
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithLocalDB()
        {
            return BlazorWasmHostedTemplate_IndividualAuth_Works(true);
        }

        [Fact]
        public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithOutLocalDB()
        {
            return BlazorWasmHostedTemplate_IndividualAuth_Works(false);
        }

        private async Task BlazorWasmHostedTemplate_IndividualAuth_Works(bool useLocalDb)
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

            var appSettings = ReadFile(serverProject.TemplateOutputDir, "appsettings.json");
            var element = JsonSerializer.Deserialize<JsonElement>(appSettings);
            var clientsProperty = element.GetProperty("IdentityServer").EnumerateObject().Single().Value.EnumerateObject().Single();
            var replacedSection = element.GetRawText().Replace(clientsProperty.Name, serverProject.ProjectName.Replace(".Server", ".Client"));
            var appSettingsPath = Path.Combine(serverProject.TemplateOutputDir, "appsettings.json");
            File.WriteAllText(appSettingsPath, replacedSection);

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
                var dbUpdateResult = await serverProject.RunDotNetEfUpdateDatabaseAsync();
                Assert.True(0 == dbUpdateResult.ExitCode, ErrorMessages.GetFailedProcessMessage("update database", serverProject, dbUpdateResult));
            }

            await BuildAndRunTest(project.ProjectName, serverProject, usesAuth: true);

            UpdatePublishedSettings(serverProject);

            if (BrowserFixture.IsHostAutomationSupported())
            {
                using var aspNetProcess = serverProject.StartPublishedProjectAsync();

                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                aspNetProcess.VisitInBrowser(Browser);
                TestBasicNavigation(project.ProjectName, usesAuth: false /* Todo: https://github.com/dotnet/runtime/issues/38154 */);
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        [Fact]
        public async Task BlazorWasmStandaloneTemplate_IndividualAuth_Works()
        {
            var project = await ProjectFactory.GetOrCreateProject("blazorstandaloneindividual", Output);
            project.TargetFramework = "netstandard2.1";

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] {
                "-au",
                "Individual",
                "--authority",
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                "--client-id",
                "sample-client-id"
            });

            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            // We don't want to test the auth flow as we don't have the required settings to talk to a third-party IdP
            // but we want to make sure that we are able to run the app without errors.
            // That will at least test that we are able to initialize and retrieve the configuration from the IdP
            // for that, we use the common microsoft tenant.
            await BuildAndRunTest(project.ProjectName, project, usesAuth: false);

            if (BrowserFixture.IsHostAutomationSupported())
            {
                var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
                using (serveProcess)
                {
                    Output.WriteLine($"Opening browser at {listeningUri}...");
                    Browser.Navigate().GoToUrl(listeningUri);
                    TestBasicNavigation(project.ProjectName);
                }
            }
            else
            {
                BrowserFixture.EnforceSupportedConfigurations();
            }
        }

        public static TheoryData<TemplateInstance> TemplateData => new TheoryData<TemplateInstance>
        {
            new TemplateInstance(
                "blazorwasmhostedaadb2c", "-ho",
                "-au", "IndividualB2C",
                "--aad-b2c-instance", "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                "--client-id", "clientId",
                "--domain", "my-domain",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmhostedaad", "-ho",
                "-au", "SingleOrg",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmstandaloneaadb2c",
                "-au", "IndividualB2C",
                "--aad-b2c-instance", "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                "--client-id", "clientId",
                "--domain", "my-domain"),
            new TemplateInstance(
                "blazorwasmstandaloneaad",
                "-au", "SingleOrg",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId"),
        };

        public class TemplateInstance
        {
            public TemplateInstance(string name, params string[] arguments)
            {
                Name = name;
                Arguments = arguments;
            }

            public string Name { get; }
            public string[] Arguments { get; }
        }

        [Theory]
        [MemberData(nameof(TemplateData))]
        public async Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_Works(TemplateInstance instance)
        {
            var project = await ProjectFactory.GetOrCreateProject(instance.Name, Output);
            project.TargetFramework = "netstandard2.1";

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: instance.Arguments);

            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));
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

        private void TestBasicNavigation(string appName, bool usesAuth = false, bool skipFetchData = false)
        {
            // Start fresh always
            if (usesAuth)
            {
                Browser.ExecuteJavaScript("sessionStorage.clear()");
                Browser.ExecuteJavaScript("localStorage.clear()");
                Browser.Manage().Cookies.DeleteAllCookies();
                Browser.Navigate().Refresh();
            }

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

            // Skipping to workaround https://github.com/dotnet/runtime/issues/38154
            skipFetchData = true;
            if (!skipFetchData)
            {
                // Can navigate to the 'fetch data' page
                Browser.FindElement(By.PartialLinkText("Fetch data")).Click();
                Browser.Contains("fetchdata", () => Browser.Url);
                Browser.Equal("Weather forecast", () => Browser.FindElement(By.TagName("h1")).Text);

                // Asynchronously loads and displays the table of weather forecasts
                Browser.Exists(By.CssSelector("table>tbody>tr"));
                Browser.Equal(5, () => Browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
            }
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
                Output = project.Output,
                DiagnosticsMessageSink = project.DiagnosticsMessageSink,
                ProjectName = projectName,
                TemplateOutputDir = subProjectDirectory,
            };

            return subProject;
        }

        private void UpdatePublishedSettings(Project serverProject)
        {
            // Hijack here the config file to use the development key during publish.
            var appSettings = JObject.Parse(File.ReadAllText(Path.Combine(serverProject.TemplateOutputDir, "appsettings.json")));
            var appSettingsDevelopment = JObject.Parse(File.ReadAllText(Path.Combine(serverProject.TemplateOutputDir, "appsettings.Development.json")));
            ((JObject)appSettings["IdentityServer"]).Merge(appSettingsDevelopment["IdentityServer"]);
            ((JObject)appSettings["IdentityServer"]).Merge(new
            {
                IdentityServer = new
                {
                    Key = new
                    {
                        FilePath = "./tempkey.json"
                    }
                }
            });
            var testAppSettings = appSettings.ToString();
            File.WriteAllText(Path.Combine(serverProject.TemplatePublishDir, "appsettings.json"), testAppSettings);
        }

        private (ProcessEx, string url) RunPublishedStandaloneBlazorProject(Project project)
        {
            var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

            Output.WriteLine("Running dotnet serve on published output...");
            var developmentCertificate = DevelopmentCertificate.Create(project.TemplateOutputDir);
            var serveProcess = ProcessEx.Run(Output, publishDir, DotNetMuxer.MuxerPathOrDefault(), $"serve -S --pfx \"{developmentCertificate.CertificatePath}\" --pfx-pwd \"{developmentCertificate.CertificatePassword}\" --port 0");
            var listeningUri = ResolveListeningUrl(serveProcess);
            return (serveProcess, listeningUri);
        }

        private static string ResolveListeningUrl(ProcessEx process)
        {
            var buffer = new List<string>();
            try
            {
                foreach (var line in process.OutputLinesAsEnumerable)
                {
                    if (line != null)
                    {
                        buffer.Add(line);
                        if (line.Trim().Contains("https://", StringComparison.Ordinal) || line.Trim().Contains("http://", StringComparison.Ordinal))
                        {
                            return line.Trim();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            throw new InvalidOperationException(@$"Couldn't find listening url:
{string.Join(Environment.NewLine, buffer.Append(process.Error))}");
        }
    }
}
