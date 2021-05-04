// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using BasicTestApp.HttpClientTest;
using OpenQA.Selenium;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Testing;
using PlaywrightSharp;
using TestServer;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.E2ETesting;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class BinaryHttpClientTest : BrowserTestBase, //PlaywrightTestBase,
        IClassFixture<BasicTestAppServerSiteFixture<CorsStartup>>,
        IClassFixture<BlazorWasmTestAppFixture<BasicTestApp.Program>>
    {
        private readonly BlazorWasmTestAppFixture<BasicTestApp.Program> _devHostServerFixture;
        readonly ServerFixture _apiServerFixture;
        IWebElement _appElement;
        IWebElement _responseStatus;
        IWebElement _responseStatusText;
        IWebElement _testOutcome;

        public BinaryHttpClientTest(
            BrowserFixture browserFixture,
            BlazorWasmTestAppFixture<BasicTestApp.Program> devHostServerFixture,
            BasicTestAppServerSiteFixture<CorsStartup> apiServerFixture,
            ITestOutputHelper output)
            : base(browserFixture, output)
            //: base(output)
        {
            _devHostServerFixture = devHostServerFixture;
            _devHostServerFixture.PathBase = "/subdir";
            _apiServerFixture = apiServerFixture;
        }

        protected override void InitializeAsyncCore(/*TestContext context*/)
        {
            //await base.InitializeCoreAsync(context);
            Browser.Navigate(_devHostServerFixture.RootUri, "/subdir", noReload: true);
            _appElement = Browser.MountTestComponent<BinaryHttpRequestsComponent>();
        }

        [Fact]
        public void CanSendAndReceiveBytes()
        {
            //if (BrowserManager.IsAvailable(BrowserKind.Chromium))
            //{
            //    await using var browser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
            //    var page = await browser.NewPageAsync();
            //    var response = await page.GoToAsync(_devHostServerFixture.RootUri + "");

            //    Assert.True(response.Ok, "Got: "+response.StatusText);

                //var socket = BrowserContextInfo.Pages[page].WebSockets.SingleOrDefault() ??
                //    (await page.WaitForEventAsync(PageEvent.WebSocket)).WebSocket;

                //// Receive render batch
                //await socket.WaitForEventAsync(WebSocketEvent.FrameReceived);
                //await socket.WaitForEventAsync(WebSocketEvent.FrameSent);

                //// JS interop call to intercept navigation
                //await socket.WaitForEventAsync(WebSocketEvent.FrameReceived);
                //await socket.WaitForEventAsync(WebSocketEvent.FrameSent);

                //await page.WaitForSelectorAsync("ul");

                //await page.CloseAsync();


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

            _responseStatus = Browser.Exists(By.Id("response-status"));
            _responseStatusText = _appElement.FindElement(By.Id("response-status-text"));
            _testOutcome = _appElement.FindElement(By.Id("test-outcome"));

        }

        private void SetValue(string elementId, string value)
        {
            var element = Browser.Exists(By.Id(elementId));
            element.Clear();
            element.SendKeys(value);
        }

    }
}
