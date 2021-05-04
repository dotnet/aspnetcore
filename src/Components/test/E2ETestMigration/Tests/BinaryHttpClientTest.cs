// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicTestApp.HttpClientTest;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Testing;
using PlaywrightSharp;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class BinaryHttpClientTest : PlaywrightTestBase,
        IClassFixture<BasicTestAppServerSiteFixture<CorsStartup>>,
        IClassFixture<BlazorWasmTestAppFixture<BasicTestApp.Program>>
    {
        private readonly BlazorWasmTestAppFixture<BasicTestApp.Program> _devHostServerFixture;
        readonly ServerFixture _apiServerFixture;

        public BinaryHttpClientTest(
            BlazorWasmTestAppFixture<BasicTestApp.Program> devHostServerFixture,
            BasicTestAppServerSiteFixture<CorsStartup> apiServerFixture,
            ITestOutputHelper output)
            : base(output)
        {
            _devHostServerFixture = devHostServerFixture;
            _devHostServerFixture.PathBase = "/subdir";
            _apiServerFixture = apiServerFixture;
        }

        [Fact]
        public async Task CanSendAndReceiveBytes()
        {
            if (BrowserManager.IsAvailable(BrowserKind.Chromium))
            {
                await using var browser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
                var page = await browser.NewPageAsync();
                var url = _devHostServerFixture.RootUri + "subdir";
                var response = await page.GoToAsync(url);

                Assert.True(response.Ok, "Got: " + response.StatusText + "from: "+url);

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
            }

            //    IssueRequest("/subdir/api/data");
            //Assert.Equal("OK", _responseStatus.Text);
            //Assert.Equal("OK", _responseStatusText.Text);
            //Assert.Equal("", _testOutcome.Text);
        }

        //private void IssueRequest(string relativeUri)
        //{
        //    var targetUri = new Uri(_apiServerFixture.RootUri, relativeUri);
        //    SetValue("request-uri", targetUri.AbsoluteUri);

        //    _appElement.FindElement(By.Id("send-request")).Click();

        //    _responseStatus = Browser.Exists(By.Id("response-status"));
        //    _responseStatusText = _appElement.FindElement(By.Id("response-status-text"));
        //    _testOutcome = _appElement.FindElement(By.Id("test-outcome"));

        //}

        //private void SetValue(string elementId, string value)
        //{
        //    var element = Browser.Exists(By.Id(elementId));
        //    element.Clear();
        //    element.SendKeys(value);
        //}

    }
}
