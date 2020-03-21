// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

// Turn off parallel test run for Edge as the driver does not support multiple Selenium tests at the same time
#if EDGE
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
#endif
namespace Templates.Test.SpaTemplateTest
{
    public class SpaTemplateTestBase : BrowserTestBase
    {
        public SpaTemplateTestBase(
            ProjectFactoryFixture projectFactory, BrowserFixture browserFixture, ITestOutputHelper output) : base(browserFixture, output)
        {
            ProjectFactory = projectFactory;
        }

        public ProjectFactoryFixture ProjectFactory { get; set; }

        public Project Project { get; set; }

        // Rather than using [Theory] to pass each of the different values for 'template',
        // it's important to distribute the SPA template tests over different test classes
        // so they can be run in parallel. Xunit doesn't parallelize within a test class.
        protected async Task SpaTemplateImplAsync(
            string key,
            string template,
            bool useLocalDb = false,
            bool usesAuth = false)
        {
            Project = await ProjectFactory.GetOrCreateProject(key, Output);

            using var createResult = await Project.RunDotNetNewAsync(template, auth: usesAuth ? "Individual" : null, language: null, useLocalDb);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            // We shouldn't have to do the NPM restore in tests because it should happen
            // automatically at build time, but by doing it up front we can avoid having
            // multiple NPM installs run concurrently which otherwise causes errors when
            // tests run in parallel.
            var clientAppSubdirPath = Path.Combine(Project.TemplateOutputDir, "ClientApp");
            ValidatePackageJson(clientAppSubdirPath);

            var projectFileContents = ReadFile(Project.TemplateOutputDir, $"{Project.ProjectName}.csproj");
            if (usesAuth && !useLocalDb)
            {
                Assert.Contains(".db", projectFileContents);
            }

            using var npmRestoreResult = await Project.RestoreWithRetryAsync(Output, clientAppSubdirPath);
            Assert.True(0 == npmRestoreResult.ExitCode, ErrorMessages.GetFailedProcessMessage("npm restore", Project, npmRestoreResult));

            using var lintResult = ProcessEx.RunViaShell(Output, clientAppSubdirPath, "npm run lint");
            Assert.True(0 == lintResult.ExitCode, ErrorMessages.GetFailedProcessMessage("npm run lint", Project, lintResult));

            // The default behavior of angular tests is watch mode, which leaves the test process open after it finishes, which leads to delays/hangs.
            var testcommand = "npm run test" + template == "angular" ? "-- --watch=false" : "";

            using var testResult = ProcessEx.RunViaShell(Output, clientAppSubdirPath, testcommand);
            Assert.True(0 == testResult.ExitCode, ErrorMessages.GetFailedProcessMessage("npm run test", Project, testResult));

            using var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            using var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            // localdb is not installed on the CI machines, so skip it.
            var shouldVisitFetchData = !(useLocalDb && Project.IsCIEnvironment);

            if (usesAuth)
            {
                using var migrationsResult = await Project.RunDotNetEfCreateMigrationAsync(template);
                Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", Project, migrationsResult));
                Project.AssertEmptyMigration(template);

                if (shouldVisitFetchData)
                {
                    using var dbUpdateResult = await Project.RunDotNetEfUpdateDatabaseAsync();
                    Assert.True(0 == dbUpdateResult.ExitCode, ErrorMessages.GetFailedProcessMessage("update database", Project, dbUpdateResult));
                }
            }

            if (template == "react" || template == "reactredux")
            {
                await CleanupReactClientAppBuildFolder(clientAppSubdirPath);
            }

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                await WarmUpServer(aspNetProcess);
                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                if (BrowserFixture.IsHostAutomationSupported())
                {
                    var (browser, logs) = await BrowserFixture.GetOrCreateBrowserAsync(Output, $"{Project.ProjectName}.build");
                    aspNetProcess.VisitInBrowser(browser);
                    TestBasicNavigation(visitFetchData: shouldVisitFetchData, usesAuth, browser, logs);
                }
                else
                {
                    BrowserFixture.EnforceSupportedConfigurations();
                }
            }

            if (usesAuth)
            {
                UpdatePublishedSettings();
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));

                await WarmUpServer(aspNetProcess);
                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                if (BrowserFixture.IsHostAutomationSupported())
                {
                    var (browser, logs) = await BrowserFixture.GetOrCreateBrowserAsync(Output, $"{Project.ProjectName}.publish");
                    aspNetProcess.VisitInBrowser(browser);
                    TestBasicNavigation(visitFetchData: shouldVisitFetchData, usesAuth, browser, logs);
                }
                else
                {
                    BrowserFixture.EnforceSupportedConfigurations();
                }
            }
        }

        private async Task CleanupReactClientAppBuildFolder(string clientAppSubdirPath)
        {
            ProcessEx testResult = null;
            int? testResultExitCode = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    testResult = ProcessEx.RunViaShell(Output, clientAppSubdirPath, "npx rimraf ./build");
                    testResultExitCode = testResult.ExitCode;
                    if (testResultExitCode == 0)
                    {
                        return;
                    }
                }
                catch
                {
                }
                finally
                {
                    testResult.Dispose();
                }

                await Task.Delay(3000);
            }

            Assert.True(testResultExitCode == 0, ErrorMessages.GetFailedProcessMessage("npx rimraf ./build", Project, testResult));
        }

        private void ValidatePackageJson(string clientAppSubdirPath)
        {
            Assert.True(File.Exists(Path.Combine(clientAppSubdirPath, "package.json")), "Missing a package.json");
            var packageJson = JObject.Parse(ReadFile(clientAppSubdirPath, "package.json"));

            // NPM package names must match ^(?:@[a-z0-9-~][a-z0-9-._~]*/)?[a-z0-9-~][a-z0-9-._~]*$
            var packageName = (string)packageJson["name"];
            Regex regex = new Regex("^(?:@[a-z0-9-~][a-z0-9-._~]*/)?[a-z0-9-~][a-z0-9-._~]*$");
            Assert.True(regex.IsMatch(packageName), "package.json name is invalid format");
        }

        private static async Task WarmUpServer(AspNetProcess aspNetProcess)
        {
            var intervalInSeconds = 5;
            var attempt = 0;
            var maxAttempts = 5;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            do
            {
                try
                {
                    attempt++;
                    var response = await aspNetProcess.SendRequest("/");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (HttpRequestException ex) when (ex.Message.StartsWith("The SSL connection could not be established"))
                {
                }
                var currentDelay = intervalInSeconds * attempt;
                await Task.Delay(TimeSpan.FromSeconds(currentDelay));
            } while (attempt < maxAttempts);
            stopwatch.Stop();
            throw new TimeoutException($"Could not contact the server within {stopwatch.Elapsed.TotalSeconds} seconds");
        }

        private void UpdatePublishedSettings()
        {
            // Hijack here the config file to use the development key during publish.
            var appSettings = JObject.Parse(File.ReadAllText(Path.Combine(Project.TemplateOutputDir, "appsettings.json")));
            var appSettingsDevelopment = JObject.Parse(File.ReadAllText(Path.Combine(Project.TemplateOutputDir, "appsettings.Development.json")));
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
            File.WriteAllText(Path.Combine(Project.TemplatePublishDir, "appsettings.json"), testAppSettings);
        }

        private void TestBasicNavigation(bool visitFetchData, bool usesAuth, IWebDriver browser, ILogs logs)
        {
            browser.Exists(By.TagName("ul"));
            // <title> element gets project ID injected into it during template execution
            browser.Contains(Project.ProjectGuid.Replace(".", "._"), () => browser.Title);

            // Initially displays the home page
            browser.Equal("Hello, world!", () => browser.FindElement(By.TagName("h1")).Text);

            // Can navigate to the counter page
            browser.Click(By.PartialLinkText("Counter"));
            browser.Contains("counter", () => browser.Url);

            browser.Equal("Counter", () => browser.FindElement(By.TagName("h1")).Text);

            // Clicking the counter button works
            browser.Equal("0", () => browser.FindElement(By.CssSelector("p>strong")).Text);
            browser.Click(By.CssSelector("p+button")) ;
            browser.Equal("1", () => browser.FindElement(By.CssSelector("p>strong")).Text);

            if (visitFetchData)
            {
                browser.Click(By.PartialLinkText("Fetch data"));

                if (usesAuth)
                {
                    // We will be redirected to the identity UI
                    browser.Contains("/Identity/Account/Login", () => browser.Url);
                    browser.Click(By.PartialLinkText("Register as a new user"));

                    var userName = $"{Guid.NewGuid()}@example.com";
                    var password = $"!Test.Password1$";
                    browser.Exists(By.Name("Input.Email"));
                    browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
                    browser.FindElement(By.Name("Input.Password")).SendKeys(password);
                    browser.FindElement(By.Name("Input.ConfirmPassword")).SendKeys(password);
                    browser.Click(By.Id("registerSubmit"));

                    // We will be redirected to the RegisterConfirmation
                    browser.Contains("/Identity/Account/RegisterConfirmation", () => browser.Url);
                    browser.Click(By.PartialLinkText("Click here to confirm your account"));

                    // We will be redirected to the ConfirmEmail
                    browser.Contains("/Identity/Account/ConfirmEmail", () => browser.Url);

                    // Now we can login
                    browser.Click(By.PartialLinkText("Login"));
                    browser.Exists(By.Name("Input.Email"));
                    browser.FindElement(By.Name("Input.Email")).SendKeys(userName);
                    browser.FindElement(By.Name("Input.Password")).SendKeys(password);
                    browser.Click(By.Id("login-submit"));

                    // Need to navigate to fetch page
                    browser.Click(By.PartialLinkText("Fetch data"));
                }

                // Can navigate to the 'fetch data' page
                browser.Contains("fetch-data", () => browser.Url);
                browser.Equal("Weather forecast", () => browser.FindElement(By.TagName("h1")).Text);

                // Asynchronously loads and displays the table of weather forecasts
                browser.Exists(By.CssSelector("table>tbody>tr"));
                browser.Equal(5, () => browser.FindElements(By.CssSelector("p+table>tbody>tr")).Count);
            }

            foreach (var logKind in logs.AvailableLogTypes)
            {
                var entries = logs.GetLog(logKind);
                var badEntries = entries.Where(e => new LogLevel[] { LogLevel.Warning, LogLevel.Severe }.Contains(e.Level));

                // Based on https://github.com/webpack/webpack-dev-server/issues/2134
                badEntries = badEntries.Where(e =>
                    !e.Message.Contains("failed: WebSocket is closed before the connection is established.")
                    && !e.Message.Contains("[WDS] Disconnected!")
                    && !e.Message.Contains("Timed out connecting to Chrome, retrying")
                    && !(e.Message.Contains("jsonp?c=") && e.Message.Contains("Uncaught TypeError:") && e.Message.Contains("is not a function")));

                Assert.True(badEntries.Count() == 0, "There were Warnings or Errors from the browser." + Environment.NewLine + string.Join(Environment.NewLine, badEntries));
            }
        }

        private void AssertFileExists(string basePath, string path, bool shouldExist)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            if (shouldExist)
            {
                Assert.True(doesExist, "Expected file to exist, but it doesn't: " + path);
            }
            else
            {
                Assert.False(doesExist, "Expected file not to exist, but it does: " + path);
            }
        }

        private string ReadFile(string basePath, string path)
        {
            AssertFileExists(basePath, path, shouldExist: true);
            return File.ReadAllText(Path.Combine(basePath, path));
        }
    }
}
