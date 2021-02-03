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
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using PlaywrightSharp;
using ProjectTemplates.Tests.Infrastructure;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BlazorWasmTemplateTest
    {
        public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory, PlaywrightFixture<BlazorServerTemplateTest> browserFixture, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Fixture = browserFixture;
            Output = output;
            BrowserContextInfo = new ContextInformation(TestHelpers.CreateFactory(output));
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }
        public PlaywrightFixture<BlazorServerTemplateTest> Fixture { get; }
        public ITestOutputHelper Output { get; }
        public ContextInformation BrowserContextInfo { get; }

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        public async Task BlazorWasmStandaloneTemplate_Works(BrowserKind browserKind)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject("blazorstandalone" + browserKind, Output);

            var createResult = await project.RunDotNetNewAsync("blazorwasm");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            // The service worker assets manifest isn't generated for non-PWA projects
            var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
            Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            await BuildAndRunTest(project.ProjectName, project, browserKind);

            var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
            using (serveProcess)
            {
                Output.WriteLine($"Opening browser at {listeningUri}...");
                if (Fixture.BrowserManager.IsAvailable(browserKind))
                {
                    await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                    var page = await NavigateToPage(browser, listeningUri);
                    await TestBasicNavigation(project.ProjectName, page);
                }
                else
                {
                    Assert.False(
                        TestHelpers.TryValidateBrowserRequired(
                            browserKind,
                            isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                            out var errorMessage),
                        errorMessage);
                }
            }
        }

        private async Task<IPage> NavigateToPage(IBrowserContext browser, string listeningUri)
        {
            var page = await browser.NewPageAsync();
            await page.GoToAsync(listeningUri, LifecycleEvent.Networkidle);
            return page;
        }

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        public async Task BlazorWasmHostedTemplate_Works(BrowserKind browserKind)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject("blazorhosted" + browserKind, Output);            
            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--hosted" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            var publishResult = await serverProject.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", serverProject, publishResult));

            var buildResult = await serverProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", serverProject, buildResult));

            await BuildAndRunTest(project.ProjectName, serverProject, browserKind);

            using var aspNetProcess = serverProject.StartPublishedProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            await AssertCompressionFormat(aspNetProcess, "br");

            if (Fixture.BrowserManager.IsAvailable(browserKind))
            {
                await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                var page = await aspNetProcess.VisitInBrowserAsync(browser);
                await TestBasicNavigation(project.ProjectName, page);
            }
            else
            {
                Assert.False(
                    TestHelpers.TryValidateBrowserRequired(
                        browserKind,
                        isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                        out var errorMessage),
                    errorMessage);
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

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        public async Task BlazorWasmStandalonePwaTemplate_Works(BrowserKind browserKind)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject("blazorstandalonepwa", Output);

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--pwa" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            await BuildAndRunTest(project.ProjectName, project, browserKind);

            ValidatePublishedServiceWorker(project);

            if (Fixture.BrowserManager.IsAvailable(browserKind))
            {
                var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
                await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                Output.WriteLine($"Opening browser at {listeningUri}...");
                var page = await NavigateToPage(browser, listeningUri);
                using (serveProcess)
                {
                    await TestBasicNavigation(project.ProjectName, page);
                }

                // The PWA template supports offline use. By now, the browser should have cached everything it needs,
                // so we can continue working even without the server.
                await page.GoToAsync("about:blank");
                await browser.SetOfflineAsync(true);
                await page.GoToAsync(listeningUri);
                await TestBasicNavigation(project.ProjectName, page, skipFetchData: true);
                await page.CloseAsync();
            }
            else
            {
                Assert.False(
                    TestHelpers.TryValidateBrowserRequired(
                        browserKind,
                        isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                        out var errorMessage),
                    errorMessage);
            }
        }

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        public async Task BlazorWasmHostedPwaTemplate_Works(BrowserKind browserKind)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject("blazorhostedpwa", Output);

            var createResult = await project.RunDotNetNewAsync("blazorwasm", args: new[] { "--hosted", "--pwa" });
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            var publishResult = await serverProject.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", serverProject, publishResult));

            var buildResult = await serverProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", serverProject, buildResult));

            await BuildAndRunTest(project.ProjectName, serverProject, browserKind);

            ValidatePublishedServiceWorker(serverProject);

            string listeningUri = null;
            if (Fixture.BrowserManager.IsAvailable(browserKind))
            {
                await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                IPage page = null;
                using (var aspNetProcess = serverProject.StartPublishedProjectAsync())
                {
                    Assert.False(
                        aspNetProcess.Process.HasExited,
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

                    await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                    page = await aspNetProcess.VisitInBrowserAsync(browser);
                    await TestBasicNavigation(project.ProjectName, page);

                    // Note: we don't want to use aspNetProcess.ListeningUri because that isn't necessarily the HTTPS URI
                    listeningUri = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
                }

                // The PWA template supports offline use. By now, the browser should have cached everything it needs,
                // so we can continue working even without the server.
                // Since this is the hosted project, backend APIs won't work offline, so we need to skip "fetchdata"
                await page.GoToAsync("about:blank");
                await browser.SetOfflineAsync(true);
                await page.GoToAsync(listeningUri);
                await TestBasicNavigation(project.ProjectName, page, skipFetchData: true);
                await page.CloseAsync();
            }
            else
            {
                Assert.False(
                    TestHelpers.TryValidateBrowserRequired(
                        browserKind,
                        isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                        out var errorMessage),
                    errorMessage);
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

        [ConditionalTheory]
        [InlineData(BrowserKind.Chromium)]
        // LocalDB doesn't work on non Windows platforms
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithLocalDB(BrowserKind browserKind)
        {
            return BlazorWasmHostedTemplate_IndividualAuth_Works(browserKind, true);
        }

        [Theory]
        [InlineData(BrowserKind.Chromium)]
        public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithOutLocalDB(BrowserKind browserKind)
        {
            return BlazorWasmHostedTemplate_IndividualAuth_Works(browserKind, false);
        }

        private async Task BlazorWasmHostedTemplate_IndividualAuth_Works(BrowserKind browserKind, bool useLocalDb)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject("blazorhostedindividual" + browserKind + (useLocalDb ? "uld" : ""), Output);

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

            await BuildAndRunTest(project.ProjectName, serverProject, browserKind, usesAuth: true);

            UpdatePublishedSettings(serverProject);

            if (Fixture.BrowserManager.IsAvailable(browserKind))
            {
                using var aspNetProcess = serverProject.StartPublishedProjectAsync();

                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                var page = await aspNetProcess.VisitInBrowserAsync(browser);
                await TestBasicNavigation(project.ProjectName, page, usesAuth: true);
                await page.CloseAsync();
            }
            else
            {
                Assert.False(
                    TestHelpers.TryValidateBrowserRequired(
                        browserKind,
                        isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                        out var errorMessage),
                    errorMessage);
            }
        }

        [Theory]
        [InlineData(BrowserKind.Chromium, Skip = "https://github.com/dotnet/aspnetcore/issues/28596")]
        public async Task BlazorWasmStandaloneTemplate_IndividualAuth_Works(BrowserKind browserKind)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await ProjectFactory.GetOrCreateProject("blazorstandaloneindividual" + browserKind, Output);

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
            await BuildAndRunTest(project.ProjectName, project, browserKind, usesAuth: false);

            var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
            using (serveProcess)
            {
                Output.WriteLine($"Opening browser at {listeningUri}...");
                await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                var page = await NavigateToPage(browser, listeningUri);
                await TestBasicNavigation(project.ProjectName, page);
                await page.CloseAsync();
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
                "blazorwasmhostedaadgraph", "-ho",
                "-au", "SingleOrg",
                "--calls-graph",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmhostedaadapi", "-ho",
                "-au", "SingleOrg",
                "--called-api-url", "\"https://graph.microsoft.com\"",
                "--called-api-scopes", "user.readwrite",
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

        protected async Task BuildAndRunTest(string appName, Project project, BrowserKind browserKind, bool usesAuth = false)
        {
            using var aspNetProcess = project.StartBuiltProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            if (Fixture.BrowserManager.IsAvailable(browserKind))
            {
                await using var browser = await Fixture.BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                var page = await aspNetProcess.VisitInBrowserAsync(browser);
                await TestBasicNavigation(appName, page, usesAuth);
                await page.CloseAsync();
            }
            else
            {
                Assert.False(
                    TestHelpers.TryValidateBrowserRequired(
                        browserKind,
                        isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                        out var errorMessage),
                    errorMessage);
            }
        }

        private async Task TestBasicNavigation(string appName, IPage page, bool usesAuth = false, bool skipFetchData = false)
        {
            await page.WaitForSelectorAsync("ul");

            // <title> element gets project ID injected into it during template execution
            Assert.Equal(appName.Trim(), (await page.GetTitleAsync()).Trim());

            // Initially displays the home page
            await page.WaitForSelectorAsync("h1 >> text=Hello, world!");

            // Can navigate to the counter page
            await Task.WhenAll(
                page.WaitForNavigationAsync("**/counter"),
                page.WaitForSelectorAsync("h1 >> text=Counter"),
                page.WaitForSelectorAsync("p >> text=Current count: 0"),
                page.ClickAsync("a[href=counter]"));

            // Clicking the counter button works
            await Task.WhenAll(
                page.WaitForSelectorAsync("p >> text=Current count: 1"),
                page.ClickAsync("p+button"));

            if (usesAuth)
            {
                await Task.WhenAll(
                    page.WaitForNavigationAsync("**/Identity/Account/Login**", LifecycleEvent.Networkidle),
                    page.ClickAsync("text=Log in"));

                await Task.WhenAll(
                    page.WaitForSelectorAsync("[name=\"Input.Email\"]"),
                    page.WaitForNavigationAsync("**/Identity/Account/Register**", LifecycleEvent.Networkidle),
                    page.ClickAsync("text=Register as a new user"));

                var userName = $"{Guid.NewGuid()}@example.com";
                var password = $"!Test.Password1$";

                await page.TypeAsync("[name=\"Input.Email\"]", userName);
                await page.TypeAsync("[name=\"Input.Password\"]", password);
                await page.TypeAsync("[name=\"Input.ConfirmPassword\"]", password);

                // We will be redirected to the RegisterConfirmation
                await Task.WhenAll(
                    page.WaitForNavigationAsync("**/Identity/Account/RegisterConfirmation**", LifecycleEvent.Networkidle),
                    page.ClickAsync("#registerSubmit"));

                // We will be redirected to the ConfirmEmail
                await Task.WhenAll(
                    page.WaitForNavigationAsync("**/Identity/Account/ConfirmEmail**", LifecycleEvent.Networkidle),
                    page.ClickAsync("text=Click here to confirm your account"));

                // Now we can login
                await page.ClickAsync("text=Login");
                await page.WaitForSelectorAsync("[name=\"Input.Email\"]");
                await page.TypeAsync("[name=\"Input.Email\"]", userName);
                await page.TypeAsync("[name=\"Input.Password\"]", password);
                await page.ClickAsync("#login-submit");

                // Need to navigate to fetch page
                await page.GoToAsync(new Uri(page.Url).GetLeftPart(UriPartial.Authority));
                Assert.Equal(appName.Trim(), (await page.GetTitleAsync()).Trim());
            }

            if (!skipFetchData)
            {
                // Can navigate to the 'fetch data' page
                await Task.WhenAll(
                    page.WaitForNavigationAsync("**/fetchdata"),
                    page.WaitForSelectorAsync("text=Weather forecast"),
                    page.ClickAsync("text=Fetch data"));

                Assert.Equal("Weather forecast", await page.GetInnerTextAsync("h1"));

                // Asynchronously loads and displays the table of weather forecasts
                await page.WaitForSelectorAsync("table>tbody>tr");
                Assert.Equal(5, (await page.QuerySelectorAllAsync("p+table>tbody>tr")).Count());
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
