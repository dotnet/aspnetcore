using OpenQA.Selenium;
using Templates.Test.Helpers;
using Xunit;

namespace Templates.Test
{
    public class SpaTemplateTest : TemplateTestBase
    {
        [Theory]
        [InlineData(null, "angular")]
        [InlineData(null, "react")]
        [InlineData(null, "reactredux")]
        [InlineData(null, "aurelia")]
        [InlineData(null, "knockout")]
        [InlineData(null, "vue")]
        // Just use 'angular' as representative for .NET 4.6.1 coverage, as
        // the client-side code isn't affected by the .NET runtime choice
        [InlineData("net461", "angular")]
        public void SpaTemplate_Works(string targetFrameworkOverride, string template)
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

            // Loads and displays the table of weather forecasts
            var fetchDataComponent = browser.FindElement("h1").Parent();
            var table = browser.FindElement(fetchDataComponent, "table", 5);
            Assert.Equal(5, table.FindElements(By.CssSelector("tbody tr")).Count);
        }
    }
}
