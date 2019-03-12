// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using ProjectTemplates.Tests.Helpers;
using System.IO;
using System.Net;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

// Turn off parallel test run for Edge as the driver does not support multiple Selenium tests at the same time
#if EDGE
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
#endif
[assembly: TestFramework("Microsoft.AspNetCore.E2ETesting.XunitTestFrameworkWithAssemblyFixture", "ProjectTemplates.Tests")]
namespace Templates.Test.SpaTemplateTest
{
    public class SpaTemplateTestBase : BrowserTestBase
    {
        public SpaTemplateTestBase(
            ProjectFactoryFixture projectFactory, BrowserFixture browserFixture, ITestOutputHelper output) : base(browserFixture, output)
        {
            Project = projectFactory.CreateProject(output);
        }

        public Project Project { get; }

        // Rather than using [Theory] to pass each of the different values for 'template',
        // it's important to distribute the SPA template tests over different test classes
        // so they can be run in parallel. Xunit doesn't parallelize within a test class.
        protected void SpaTemplateImpl(string template, bool noHttps = false)
        {
            Project.RunDotNetNew(template, noHttps: noHttps);

            // For some SPA templates, the NPM root directory is './ClientApp'. In other
            // templates it's at the project root. Strictly speaking we shouldn't have
            // to do the NPM restore in tests because it should happen automatically at
            // build time, but by doing it up front we can avoid having multiple NPM
            // installs run concurrently which otherwise causes errors when tests run
            // in parallel.
            var clientAppSubdirPath = Path.Combine(Project.TemplateOutputDir, "ClientApp");
            Assert.True(File.Exists(Path.Combine(clientAppSubdirPath, "package.json")), "Missing a package.json");

            Npm.RestoreWithRetry(Output, clientAppSubdirPath);
            Npm.Test(Output, clientAppSubdirPath);

            TestApplication(publish: false);
            TestApplication(publish: true);
        }

        private void TestApplication(bool publish)
        {
            using (var aspNetProcess = Project.StartAspNetProcess(publish))
            {
                aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                if (BrowserFixture.IsHostAutomationSupported())
                {
                    aspNetProcess.VisitInBrowser(Browser);
                    TestBasicNavigation();
                }
            }
        }

        private void TestBasicNavigation()
        {
            Browser.WaitForElement("ul");
            // <title> element gets project ID injected into it during template execution
            Assert.Contains(Project.ProjectGuid, Browser.Title);

            // Initially displays the home page
            Assert.Equal("Hello, world!", Browser.GetText("h1"));

            // Can navigate to the counter page
            Browser.Click(By.PartialLinkText("Counter"));
            Browser.WaitForUrl("counter");

            Assert.Equal("Counter", Browser.GetText("h1"));

            // Clicking the counter button works
            var counterComponent = Browser.FindElement("h1").Parent();
            Assert.Equal("0", counterComponent.GetText("strong"));
            Browser.Click(counterComponent, "button");
            Assert.Equal("1", counterComponent.GetText("strong"));

            // Can navigate to the 'fetch data' page
            Browser.Click(By.PartialLinkText("Fetch data"));
            Browser.WaitForUrl("fetch-data");
            Assert.Equal("Weather forecast", Browser.GetText("h1"));

            // Asynchronously loads and displays the table of weather forecasts
            var fetchDataComponent = Browser.FindElement("h1").Parent();
            Browser.WaitForElement("table>tbody>tr");
            var table = Browser.FindElement(fetchDataComponent, "table", timeoutSeconds: 5);
            Assert.Equal(5, table.FindElements(By.CssSelector("tbody tr")).Count);
        }
    }
}
