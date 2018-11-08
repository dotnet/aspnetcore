using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Components.FunctionalTests
{
    public class SampleTest :
        IClassFixture<AspNetSiteServerFixture<ComponentsWebSite.Program>>,
        IClassFixture<BrowserFixture>
    {
        public SampleTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture<ComponentsWebSite.Program> serverFixture)
        {
            Browser = browserFixture.Browser;
            RootUri = serverFixture.RootUri;
        }

        public IWebDriver Browser { get; }

        public Uri RootUri { get; }

        [Fact]
        public void CanLoadPage()
        {
            // Arrange
            Browser.Url = RootUri.ToString();

            // Act
            Browser.Navigate();

            // Assert
            Assert.Contains("Hello world", Browser.PageSource);
        }
    }
}
