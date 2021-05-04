// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class SignalRClientTest : ServerTestBase<BlazorWasmTestAppFixture<BasicTestApp.Program>>,
        IClassFixture<BasicTestAppServerSiteFixture<CorsStartup>>
    {
        private readonly ServerFixture _apiServerFixture;

        public SignalRClientTest(
            BrowserFixture browserFixture,
            BlazorWasmTestAppFixture<BasicTestApp.Program> devHostServerFixture,
            BasicTestAppServerSiteFixture<CorsStartup> apiServerFixture,
            ITestOutputHelper output)
            : base(browserFixture, devHostServerFixture, output)
        {
            _serverFixture.PathBase = "/subdir";
            _apiServerFixture = apiServerFixture;
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase);
            Browser.MountTestComponent<SignalRClientComponent>();
            Browser.Exists(By.Id("signalr-client"));
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/27156")]
        public void SignalRClientWorksWithLongPolling()
        {
            Browser.Exists(By.Id("hub-url")).SendKeys(
                new Uri(_apiServerFixture.RootUri, "/subdir/chathub").AbsoluteUri);
            var target = new SelectElement(Browser.Exists(By.Id("transport-type")));
            target.SelectByText("LongPolling");
            Browser.Exists(By.Id("hub-connect")).Click();

            Browser.Equal("SignalR Client: Echo LongPolling",
                () => Browser.FindElements(By.CssSelector("li")).FirstOrDefault()?.Text);
        }

        [Fact]
        public void SignalRClientWorksWithWebSockets()
        {
            Browser.Exists(By.Id("hub-url")).SendKeys(
                new Uri(_apiServerFixture.RootUri, "/subdir/chathub").AbsoluteUri);
            var target = new SelectElement(Browser.Exists(By.Id("transport-type")));
            target.SelectByText("WebSockets");
            Browser.Exists(By.Id("hub-connect")).Click();

            Browser.Equal("SignalR Client: Echo WebSockets",
                () => Browser.FindElements(By.CssSelector("li")).FirstOrDefault()?.Text);
        }

        [Fact]
        public void SignalRClientSendsUserAgent()
        {
            Browser.Exists(By.Id("hub-url")).SendKeys(
                new Uri(_apiServerFixture.RootUri, "/subdir/chathub").AbsoluteUri);
            var target = new SelectElement(Browser.Exists(By.Id("transport-type")));
            target.SelectByText("LongPolling");
            Browser.Exists(By.Id("hub-connect")).Click();

            Browser.Equal("SignalR Client: Echo LongPolling",
                () => Browser.FindElements(By.CssSelector("li")).FirstOrDefault()?.Text);

            Browser.Exists(By.Id("hub-useragent")).Click();
            Assert.NotNull(Browser.FindElement(By.Id("useragent")).Text);
        }
    }
}
