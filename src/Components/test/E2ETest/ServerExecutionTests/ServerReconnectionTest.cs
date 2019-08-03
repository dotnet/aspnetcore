// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ServerReconnectionTest : BasicTestAppTestBase
    {
        public ServerReconnectionTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {
        }

        public string SessionIdentifier { get; set; } = Guid.NewGuid().ToString();

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: false);

            Browser.Manage().Cookies.DeleteCookieNamed("WebSockets.Identifier");
            Browser.Manage().Cookies.AddCookie(new Cookie("WebSockets.Identifier", SessionIdentifier));
            Browser.Navigate().Refresh();
        }

        [Fact]
        public void ReconnectUI()
        {
            MountTestComponent<ReconnectComponent>();
            Browser.Equal("0", () => Browser.FindElement(By.Id("counter-count")).Text);

            var counterButton = Browser.FindElement(By.Id("counter-click"));
            for (int i = 0; i < 10; i++)
            {
                counterButton.Click();
            }

            Disconnect();

            // We should see the 'reconnecting' UI appear
            Browser.True(
                () => Browser.FindElement(By.Id("components-reconnect-modal"))?.GetCssValue("display") == "block",
                TimeSpan.FromSeconds(10));

            // Then it should disappear
            Browser.True(() => Browser.FindElement(By.Id("components-reconnect-modal"))?.GetCssValue("display") == "none",
                TimeSpan.FromSeconds(10));

            counterButton = Browser.FindElement(By.Id("counter-click"));
            for (int i = 0; i < 10; i++)
            {
                counterButton.Click();
            }

            Browser.Equal("20", () => Browser.FindElement(By.Id("counter-count")).Text);
        }

        [Fact]
        public void RendersContinueAfterReconnect()
        {
            MountTestComponent<ReconnectTicker>();

            var selector = By.ClassName("tick-value");
            var element = Browser.FindElement(selector);

            var initialValue = element.Text;

            Disconnect();

            // We should see the 'reconnecting' UI appear
            Browser.True(
                () => Browser.FindElement(By.Id("components-reconnect-modal"))?.GetCssValue("display") == "block",
                TimeSpan.FromSeconds(10));

            // Then it should disappear
            Browser.True(() => Browser.FindElement(By.Id("components-reconnect-modal"))?.GetCssValue("display") == "none",
                TimeSpan.FromSeconds(10));

            // We should receive a render that occurred while disconnected
            var currentValue = element.Text;
            Assert.NotEqual(initialValue, currentValue);

            // Verify it continues to tick
            new WebDriverWait(Browser, TimeSpan.FromSeconds(10)).Until(
                _ => element.Text != currentValue);
        }

        private void Disconnect()
        {
            var javascript = (IJavaScriptExecutor)Browser;
            Browser.ExecuteAsyncScript($"fetch('/WebSockets/Interrupt?WebSockets.Identifier={SessionIdentifier}').then(r => window['WebSockets.{SessionIdentifier}'] = r.ok)");
            Browser.HasJavaScriptValue(true, $"window['WebSockets.{SessionIdentifier}']", (r) => r != null);
        }
    }
}
