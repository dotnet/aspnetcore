// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
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
        protected void SpaTemplateImpl(string targetFrameworkOverride, string template)
        {
            RunDotNetNew(template, targetFrameworkOverride);
            RunNpmInstall();
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
            Assert.Contains(ProjectName, browser.Title);

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
