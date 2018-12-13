// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using OpenQA.Selenium;
using System.IO;
using System.Net;
using Templates.Test.Helpers;
using Templates.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorComponentsTemplateTest : BrowserTestBase
    {
        public RazorComponentsTemplateTest(BrowserFixture browserFixture, ITestOutputHelper output) : base(browserFixture, output)
        {
        }

        [Fact]
        public void RazorComponentsTemplateWorks()
        {
            RunDotNetNew("razorcomponents");

            // We don't want the Directory.Build.props/targets interfering
            File.WriteAllText(
                Path.Combine(TemplateOutputDir, "Directory.Build.props"),
                "<Project />");
            File.WriteAllText(
                Path.Combine(TemplateOutputDir, "Directory.Build.targets"),
                "<Project />");

            // Run the "server" project
            ProjectName += ".Server";
            TemplateOutputDir = Path.Combine(TemplateOutputDir, ProjectName);

            TestApplication(publish: false);
            TestApplication(publish: true);
        }

        private void TestApplication(bool publish)
        {
            using (var aspNetProcess = StartAspNetProcess(publish))
            {
                aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

                if (WebDriverFactory.HostSupportsBrowserAutomation)
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
            Assert.Contains(ProjectGuid, Browser.Title);

            // Initially displays the home page
            Assert.Equal("Hello, world!", Browser.GetText("h1"));

            // Can navigate to the counter page
            Browser.Click(By.PartialLinkText("Counter"));
            Browser.WaitForUrl("counter");
            Browser.WaitForText("h1", "Counter");

            // Clicking the counter button works
            var counterComponent = Browser.FindElement("h1").Parent();
            var counterDisplay = Browser.FindElement("h1 + p");
            Assert.Equal("Current count: 0", counterDisplay.Text);
            Browser.Click(counterComponent, "button");
            Assert.Equal("Current count: 1", counterDisplay.Text);

            // Can navigate to the 'fetch data' page
            Browser.Click(By.PartialLinkText("Fetch data"));
            Browser.WaitForUrl("fetchdata");
            Browser.WaitForText("h1", "Weather forecast");

            // Asynchronously loads and displays the table of weather forecasts
            var fetchDataComponent = Browser.FindElement("h1").Parent();
            Browser.WaitForElement("table>tbody>tr");
            var table = Browser.FindElement(fetchDataComponent, "table", timeoutSeconds: 5);
            Assert.Equal(5, table.FindElements(By.CssSelector("tbody tr")).Count);
        }
    }
}
