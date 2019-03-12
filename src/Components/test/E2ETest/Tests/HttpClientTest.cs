// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp.HttpClientTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class HttpClientTest : BasicTestAppTestBase, IClassFixture<AspNetSiteServerFixture>
    {
        readonly ServerFixture _apiServerFixture;
        readonly IWebElement _appElement;
        IWebElement _responseStatus;
        IWebElement _responseBody;
        IWebElement _responseHeaders;

        public HttpClientTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<BasicTestApp.Program> devHostServerFixture,
            AspNetSiteServerFixture apiServerFixture,
            ITestOutputHelper output)
            : base(browserFixture, devHostServerFixture, output)
        {
            apiServerFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
            _apiServerFixture = apiServerFixture;

            Navigate(ServerPathBase, noReload: true);
            _appElement = MountTestComponent<HttpRequestsComponent>();
        }

        [Fact]
        public async Task SanityCheck_ApiServerIsRunning()
        {
            // Just so we can be sure that the other tests are even relevant
            // Note that the HttpClient we're instantiating here is *not* the
            // one under test. This is not related to Blazor in any way.
            var httpClient = new HttpClient { BaseAddress = _apiServerFixture.RootUri };
            var responseText = await httpClient.GetStringAsync("/api/greeting/sayhello");
            Assert.Equal("Hello", responseText);
        }

        [Fact]
        public void CanPerformGetRequest()
        {
            IssueRequest("GET", "/api/person");
            Assert.Equal("OK", _responseStatus.Text);
            Assert.Equal("[\"value1\",\"value2\"]", _responseBody.Text);
        }

        [Fact]
        public void CanPerformPostRequestWithBody()
        {
            var testMessage = $"The value is {Guid.NewGuid()}";
            IssueRequest("POST", "/api/person", testMessage);
            Assert.Equal("OK", _responseStatus.Text);
            Assert.Equal($"You posted: {testMessage}", _responseBody.Text);
        }

        [Fact]
        public void CanReadResponseHeaders()
        {
            IssueRequest("GET", "/api/person");
            Assert.Equal("OK", _responseStatus.Text);

            // Note that we only see header names case insensitively. The 'fetch' API
            // can use whatever casing rules it wants, because the HTTP spec says the
            // names are case-insensitive. In practice, Chrome lowercases them all.
            // Ideally we should make the test case-insensitive for header name, but
            // case-sensitive for header value.
            Assert.Contains("mycustomheader: My custom value", _responseHeaders.Text);
        }

        [Fact]
        public void CanSendRequestHeaders()
        {
            AddRequestHeader("TestHeader", "Value from test");
            AddRequestHeader("another-header", "Another value");
            IssueRequest("DELETE", "/api/person");
            Assert.Equal("OK", _responseStatus.Text);
            Assert.Contains("TestHeader: Value from test", _responseBody.Text);
            Assert.Contains("another-header: Another value", _responseBody.Text);
        }

        [Fact]
        public void CanSendAndReceiveJson()
        {
            AddRequestHeader("Content-Type", "application/json");
            IssueRequest("PUT", "/api/person", "{\"Name\": \"Bert\", \"Id\": 123}");
            Assert.Equal("OK", _responseStatus.Text);
            Assert.Contains("Content-Type: application/json", _responseHeaders.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("{\"id\":123,\"name\":\"Bert\"}", _responseBody.Text);
        }

        [Fact]
        public void CanSetRequestReferer()
        {
            SetValue("request-referrer", "/test-referrer");
            IssueRequest("GET", "/api/person/referrer");
            Assert.Equal("OK", _responseStatus.Text);
            Assert.EndsWith("/test-referrer", _responseBody.Text);
        }

        [Fact]
        public void CanSendAndReceiveCookies()
        {
            var app = MountTestComponent<CookieCounterComponent>();
            var deleteButton = app.FindElement(By.Id("delete"));
            var incrementButton = app.FindElement(By.Id("increment"));
            app.FindElement(By.TagName("input")).SendKeys(_apiServerFixture.RootUri.ToString());

            // Ensure we're starting from a clean state
            deleteButton.Click();
            Assert.Equal("Reset completed", WaitAndGetResponseText());

            // Observe that subsequent requests manage to preserve state via cookie
            incrementButton.Click();
            Assert.Equal("Counter value is 1", WaitAndGetResponseText());
            incrementButton.Click();
            Assert.Equal("Counter value is 2", WaitAndGetResponseText());

            // Verify that attempting to delete a cookie actually works
            deleteButton.Click();
            Assert.Equal("Reset completed", WaitAndGetResponseText());
            incrementButton.Click();
            Assert.Equal("Counter value is 1", WaitAndGetResponseText());

            string WaitAndGetResponseText()
            {
                new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                    driver => driver.FindElement(By.Id("response-text")) != null);
                return app.FindElement(By.Id("response-text")).Text;
            }
        }

        private void IssueRequest(string requestMethod, string relativeUri, string requestBody = null)
        {
            var targetUri = new Uri(_apiServerFixture.RootUri, relativeUri);
            SetValue("request-uri", targetUri.AbsoluteUri);
            SetValue("request-body", requestBody ?? string.Empty);
            new SelectElement(Browser.FindElement(By.Id("request-method")))
                .SelectByText(requestMethod);

            _appElement.FindElement(By.Id("send-request")).Click();

            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.Id("response-status")) != null);
            _responseStatus = _appElement.FindElement(By.Id("response-status"));
            _responseBody = _appElement.FindElement(By.Id("response-body"));
            _responseHeaders = _appElement.FindElement(By.Id("response-headers"));
        }

        private void AddRequestHeader(string name, string value)
        {
            var addHeaderButton = _appElement.FindElement(By.Id("add-header"));
            addHeaderButton.Click();
            var newHeaderEntry = _appElement.FindElement(By.CssSelector(".header-entry:last-of-type"));
            var textBoxes = newHeaderEntry.FindElements(By.TagName("input"));
            textBoxes[0].SendKeys(name);
            textBoxes[1].SendKeys(value);
        }

        private void SetValue(string elementId, string value)
        {
            var element = Browser.FindElement(By.Id(elementId));
            element.Clear();
            element.SendKeys(value);
        }
    }
}
