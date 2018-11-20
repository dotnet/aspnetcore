// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using System.IO;
using System.Net;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.SpaTemplateTest
{
    public class SpaTemplateTestBase : TemplateTestBase
    {
        public SpaTemplateTestBase(ITestOutputHelper output) : base(output)
        {
        }

        // Rather than using [Theory] to pass each of the different values for 'template',
        // it's important to distribute the SPA template tests over different test classes
        // so they can be run in parallel. Xunit doesn't parallelize within a test class.
        protected void SpaTemplateImpl(string targetFrameworkOverride, string template, bool noHttps = false)
        {
            RunDotNetNew(template, targetFrameworkOverride, noHttps: noHttps);

            // For some SPA templates, the NPM root directory is './ClientApp'. In other
            // templates it's at the project root. Strictly speaking we shouldn't have
            // to do the NPM restore in tests because it should happen automatically at
            // build time, but by doing it up front we can avoid having multiple NPM
            // installs run concurrently which otherwise causes errors when tests run
            // in parallel.
            var clientAppSubdirPath = Path.Combine(TemplateOutputDir, "ClientApp");
            if (File.Exists(Path.Combine(clientAppSubdirPath, "package.json")))
            {
                Npm.RestoreWithRetry(Output, clientAppSubdirPath);
            }
            else if (File.Exists(Path.Combine(TemplateOutputDir, "package.json")))
            {
                Npm.RestoreWithRetry(Output, TemplateOutputDir);
            }

            TestApplication(targetFrameworkOverride, publish: false);
            TestApplication(targetFrameworkOverride, publish: true);
        }

        private void TestApplication(string targetFrameworkOverride, bool publish)
        {
            using (var aspNetProcess = StartAspNetProcess(targetFrameworkOverride, publish))
            {
                aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                if (WebDriverFactory.HostSupportsBrowserAutomation)
                {
                    using (var browser = aspNetProcess.VisitInBrowser())
                    {
                        TestBasicNavigation(browser);
                    }
                }
            }
        }

        private void TestBasicNavigation(IWebDriver browser)
        {
            // <title> element gets project ID injected into it during template execution
            Assert.Contains(ProjectGuid, browser.Title);

            // Initially displays the home page
            Assert.Equal("Hello, world!", browser.GetText("h1"));

            // Can navigate to the counter page
            browser.Click(By.PartialLinkText("Counter"));
            Assert.Equal("Counter", browser.GetText("h1"));

            // Clicking the counter button works
            var counterComponent = browser.FindElement("h1").Parent();
            Assert.Equal("0", counterComponent.GetText("strong"));
            browser.Click(counterComponent, "button");
            Assert.Equal("1", counterComponent.GetText("strong"));

            // Can navigate to the 'fetch data' page
            browser.Click(By.PartialLinkText("Fetch data"));
            Assert.Equal("Weather forecast", browser.GetText("h1"));

            // Asynchronously loads and displays the table of weather forecasts
            var fetchDataComponent = browser.FindElement("h1").Parent();
            var table = browser.FindElement(fetchDataComponent, "table", timeoutSeconds: 5);
            Assert.Equal(5, table.FindElements(By.CssSelector("tbody tr")).Count);
        }
    }
}
