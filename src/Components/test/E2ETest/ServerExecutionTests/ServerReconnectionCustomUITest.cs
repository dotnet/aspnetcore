// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp.Reconnection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerReconnectionCustomUITest : ServerReconnectionTest
{
    public ServerReconnectionCustomUITest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"{ServerPathBase}?useCustomReconnectModal=true");
        Browser.MountTestComponent<ReconnectionComponent>();
        Browser.Exists(By.Id("count"));
    }

    [Fact]
    public void CustomReconnectUI()
    {
        Browser.Exists(By.Id("increment")).Click();

        Browser.Equal("dialog", () => Browser.Exists(By.Id("components-reconnect-modal")).TagName);

        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript("Blazor._internal.forceCloseConnection()");

        // We should see the 'reconnecting' UI appear
        Browser.NotEqual(null, () => Browser.Exists(By.Id("components-reconnect-modal")).GetAttribute("open"));

        // Then it should disappear
        Browser.Equal(null, () => Browser.Exists(By.Id("components-reconnect-modal")).GetAttribute("open"));

        Browser.Exists(By.Id("increment")).Click();

        // Can dispatch events after reconnect
        Browser.Equal("2", () => Browser.Exists(By.Id("count")).Text);
    }
}
