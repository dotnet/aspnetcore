// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using BasicTestApp.HttpClientTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class BinaryHttpClientTest : BrowserTestBase,
        IClassFixture<BasicTestAppServerSiteFixture<CorsStartup>>,
        IClassFixture<DevHostServerFixture<BasicTestApp.Program>>
    {
        private readonly DevHostServerFixture<BasicTestApp.Program> _devHostServerFixture;
        readonly ServerFixture _apiServerFixture;
        IWebElement _appElement;
        IWebElement _responseStatus;
        IWebElement _responseStatusText;
        IWebElement _testOutcome;

        public BinaryHttpClientTest(
            BrowserFixture browserFixture,
            DevHostServerFixture<BasicTestApp.Program> devHostServerFixture,
            BasicTestAppServerSiteFixture<CorsStartup> apiServerFixture,
            ITestOutputHelper output)
            : base(browserFixture, output)
        {
            _devHostServerFixture = devHostServerFixture;
            _devHostServerFixture.PathBase = "/subdir";
            _apiServerFixture = apiServerFixture;
        }

        protected override void InitializeAsyncCore()
        {
            Browser.Navigate(_devHostServerFixture.RootUri, "/subdir", noReload: true);
            _appElement = Browser.MountTestComponent<BinaryHttpRequestsComponent>();
        }

        [Fact]
        public void CanSendAndReceiveBytes()
        {
            IssueRequest("/subdir/api/data");
            Assert.Equal("OK", _responseStatus.Text);
            Assert.Equal("OK", _responseStatusText.Text);
            Assert.Equal("", _testOutcome.Text);
        }

        private void IssueRequest(string relativeUri)
        {
            var targetUri = new Uri(_apiServerFixture.RootUri, relativeUri);
            SetValue("request-uri", targetUri.AbsoluteUri);

            _appElement.FindElement(By.Id("send-request")).Click();

            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.Id("response-status")) != null);
            _responseStatus = _appElement.FindElement(By.Id("response-status"));
            _responseStatusText = _appElement.FindElement(By.Id("response-status-text"));
            _testOutcome = _appElement.FindElement(By.Id("test-outcome"));
        }

        private void SetValue(string elementId, string value)
        {
            var element = Browser.FindElement(By.Id(elementId));
            element.Clear();
            element.SendKeys(value);
        }
    }
}
