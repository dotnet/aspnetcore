// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using BasicTestApp;
using Components.TestServer;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class ProtectedBrowserStorageBrowserTest : ServerTestBase<BasicTestAppServerSiteFixture<ProtectedBrowserStorageStartup>>
    {
        public ProtectedBrowserStorageBrowserTest(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<ProtectedBrowserStorageStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        public override async Task InitializeAsync()
        {
            // Since browser storage needs to be reset in between tests, it's easiest for each
            // test to run in its own browser instance.
            await base.InitializeAsync(Guid.NewGuid().ToString());
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase);
            Browser.MountTestComponent<ProtectedBrowserStorageComponent>();
        }

        [Fact]
        public void LocalStoragePersistsOnRefresh()
        {
            // Local storage initially cleared
            var incrementLocalButton = Browser.FindElement(By.Id("increment-local"));
            var localCount = Browser.FindElement(By.Id("local-count"));
            Browser.Equal("0", () => localCount.Text);

            // Local storage updates
            incrementLocalButton.Click();
            Browser.Equal("1", () => localCount.Text);

            // Local storage persists on refresh
            Browser.Navigate().Refresh();
            Browser.MountTestComponent<ProtectedBrowserStorageComponent>();

            localCount = Browser.FindElement(By.Id("local-count"));
            Browser.Equal("1", () => localCount.Text);
        }

        [Fact]
        public void LocalStoragePersistsAccrossTabs()
        {
            // Local storage initially cleared
            var incrementLocalButton = Browser.FindElement(By.Id("increment-local"));
            var localCount = Browser.FindElement(By.Id("local-count"));
            Browser.Equal("0", () => localCount.Text);

            // Local storage updates in current tab
            incrementLocalButton.Click();
            Browser.Equal("1", () => localCount.Text);

            // Local storage persists accross tabs
            OpenNewSession();
            localCount = Browser.FindElement(By.Id("local-count"));
            Browser.Equal("1", () => localCount.Text);
        }

        [Fact]
        public void SessionStoragePersistsOnRefresh()
        {
            // Session storage initially cleared
            var incrementSessionButton = Browser.FindElement(By.Id("increment-session"));
            var sessionCount = Browser.FindElement(By.Id("session-count"));
            Browser.Equal("0", () => sessionCount.Text);

            // Session storage updates
            incrementSessionButton.Click();
            Browser.Equal("1", () => sessionCount.Text);

            // Session storage persists on refresh
            Browser.Navigate().Refresh();
            Browser.MountTestComponent<ProtectedBrowserStorageComponent>();

            sessionCount = Browser.FindElement(By.Id("session-count"));
            Browser.Equal("1", () => sessionCount.Text);
        }

        [Fact]
        public void SessionStorageDoesNotPersistAccrossTabs()
        {
            // Session storage initially cleared
            var incrementSessionButton = Browser.FindElement(By.Id("increment-session"));
            var sessionCount = Browser.FindElement(By.Id("session-count"));
            Browser.Equal("0", () => sessionCount.Text);

            // Session storage updates in current tab
            incrementSessionButton.Click();
            Browser.Equal("1", () => sessionCount.Text);

            // Session storage does not persist accross tabs
            OpenNewSession();
            sessionCount = Browser.FindElement(By.Id("session-count"));
            Browser.Equal("0", () => sessionCount.Text);
        }

        /// <summary>
        /// Opens a new session in a new tab, mounting a new test component.
        /// </summary>
        /// <remarks>
        /// Simply opening a new tab using JS is not sufficient because the browser context perists and
        /// the same session is maintained. The way this method starts a new session is by simulating a
        /// ctrl+click (or command+click on Mac) on a link that opens a new tab. This opens it as a background
        /// tab, which has a new browser context.
        /// </remarks>
        private void OpenNewSession()
        {
            var modifierKey = Browser is RemoteWebDriver remoteBrowser && remoteBrowser.Capabilities.Platform.PlatformType == PlatformType.Mac ?
                Keys.Command :
                Keys.Control;

            var newTabLink = Browser.FindElement(By.Id("new-tab"));
            var action = new Actions(Browser);
            action.KeyDown(modifierKey).MoveToElement(newTabLink).Click().KeyUp(modifierKey).Perform();

            Browser.SwitchTo().Window(Browser.WindowHandles.Last());

            Navigate(ServerPathBase);
            Browser.MountTestComponent<ProtectedBrowserStorageComponent>();
        }
    }
}
