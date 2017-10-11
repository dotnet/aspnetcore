// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using OpenQA.Selenium;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class SpaTemplateTest : TemplateTestBase
    {
        public SpaTemplateTest(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        // Just use 'angular' as representative for .NET 4.6.1 coverage, as
        // the client-side code isn't affected by the .NET runtime choice
        [InlineData("angular")]
        public void SpaTemplate_Works_NetFramework(string template)
            => SpaTemplateImpl("net461", template);

        [Theory]
        [InlineData("angular")]
        [InlineData("react")]
        [InlineData("reactredux")]
        [InlineData("aurelia")]
        [InlineData("knockout")]
        [InlineData("vue")]
        public void SpaTemplate_Works_NetCore(string template)
            => SpaTemplateImpl(null, template);

        private void SpaTemplateImpl(string targetFrameworkOverride, string template)
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
                aspNetProcess.AssertOk("/");

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
