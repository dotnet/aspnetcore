// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Components.TestServer;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ProtectedBrowserStorageUsageTest : ServerTestBase<ToggleExecutionModeServerFixture<BasicTestApp.Program>>
{
    public ProtectedBrowserStorageUsageTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<BasicTestApp.Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture.WithServerExecution(), output)
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
        Browser.MountTestComponent<ProtectedBrowserStorageUsageComponent>();
    }

    [Fact]
    public void LocalStoragePersistsOnRefresh()
    {
        // Local storage initially cleared
        var incrementLocalButton = Browser.Exists(By.Id("increment-local"));
        var localCount = Browser.Exists(By.Id("local-count"));
        Browser.Equal("0", () => localCount.Text);

        // Local storage updates
        incrementLocalButton.Click();
        Browser.Equal("1", () => localCount.Text);

        // Local storage persists on refresh
        Browser.Navigate().Refresh();
        Browser.MountTestComponent<ProtectedBrowserStorageUsageComponent>();

        localCount = Browser.Exists(By.Id("local-count"));
        Browser.Equal("1", () => localCount.Text);
    }

    [Fact]
    public void LocalStoragePersistsAcrossTabs()
    {
        // Local storage initially cleared
        var incrementLocalButton = Browser.Exists(By.Id("increment-local"));
        var localCount = Browser.Exists(By.Id("local-count"));
        Browser.Equal("0", () => localCount.Text);

        // Local storage updates in current tab
        incrementLocalButton.Click();
        Browser.Equal("1", () => localCount.Text);

        // Local storage persists across tabs
        OpenNewSession();
        localCount = Browser.Exists(By.Id("local-count"));
        Browser.Equal("1", () => localCount.Text);
    }

    [Fact]
    public void SessionStoragePersistsOnRefresh()
    {
        // Session storage initially cleared
        var incrementSessionButton = Browser.Exists(By.Id("increment-session"));
        var sessionCount = Browser.Exists(By.Id("session-count"));
        Browser.Equal("0", () => sessionCount.Text);

        // Session storage updates
        incrementSessionButton.Click();
        Browser.Equal("1", () => sessionCount.Text);

        // Session storage persists on refresh
        Browser.Navigate().Refresh();
        Browser.MountTestComponent<ProtectedBrowserStorageUsageComponent>();

        sessionCount = Browser.Exists(By.Id("session-count"));
        Browser.Equal("1", () => sessionCount.Text);
    }

    [Fact]
    public void SessionStorageDoesNotPersistAcrossTabs()
    {
        // Session storage initially cleared
        var incrementSessionButton = Browser.Exists(By.Id("increment-session"));
        var sessionCount = Browser.Exists(By.Id("session-count"));
        Browser.Equal("0", () => sessionCount.Text);

        // Session storage updates in current tab
        incrementSessionButton.Click();
        Browser.Equal("1", () => sessionCount.Text);

        // Session storage does not persist across tabs
        OpenNewSession();
        sessionCount = Browser.Exists(By.Id("session-count"));
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
        var modifierKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            Keys.Command :
            Keys.Control;

        var newTabLink = Browser.Exists(By.Id("new-tab"));
        var action = new Actions(Browser);
        action.KeyDown(modifierKey).MoveToElement(newTabLink).Click().KeyUp(modifierKey).Perform();

        Browser.SwitchTo().Window(Browser.WindowHandles.Last());

        Navigate(ServerPathBase);
        Browser.MountTestComponent<ProtectedBrowserStorageUsageComponent>();
    }
}
