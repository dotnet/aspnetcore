// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.CommandLineUtils;
using OpenQA.Selenium;
using ProjectTemplates.Tests.Helpers;
using System.IO;
using System.Net;
using System.Threading;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class RazorComponentsTemplateTest : BrowserTestBase
    {
        public RazorComponentsTemplateTest(ProjectFactoryFixture projectFactory, BrowserFixture browserFixture, ITestOutputHelper output) : base(browserFixture, output)
        {
            Project = projectFactory.CreateProject(output);
        }

        public Project Project { get; }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/8244")]
        public void RazorComponentsTemplateWorks()
        {
            Project.RunDotNetNew("razorcomponents");
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
            // Give components.server enough time to load so that it can replace
            // the prerendered content before we start making assertions.
            Thread.Sleep(5000);
            Browser.WaitForElement("ul");
            // <title> element gets project ID injected into it during template execution
            Assert.Contains(Project.ProjectGuid, Browser.Title);

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
            WaitAssert.Equal("Current count: 1", () => Browser.FindElement("h1+p").Text);

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
