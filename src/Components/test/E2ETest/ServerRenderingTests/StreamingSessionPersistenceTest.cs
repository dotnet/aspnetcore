// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests;

public class StreamingSessionPersistenceTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    private const string SessionCookieName = ".AspNetCore.Session";

    public StreamingSessionPersistenceTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        _serverFixture.AdditionalArguments.Add("--UseSessionStorageTempDataProvider=true");
        _serverFixture.AdditionalArguments.Add("--UseSession=true");
        Browser.Manage().Cookies.DeleteCookieNamed(SessionCookieName);
        base.InitializeAsyncCore();
    }

    [Fact]
    public void StreamingSSR_PersistsSupplyParameterFromSession_AfterAsyncRendering()
    {
        Navigate($"{ServerPathBase}/streaming-session-persistence");

        // Wait for streaming to complete — values are set AFTER an await in OnInitializedAsync
        Browser.Exists(By.Id("streaming-complete"));
        Browser.Equal("set-during-streaming", () => Browser.FindElement(By.Id("session-value")).Text);

        // Navigate to the read page with a full page load to verify the value survived the request boundary
        Navigate($"{ServerPathBase}/streaming-session-persistence/read");
        Browser.Equal("set-during-streaming", () => Browser.FindElement(By.Id("read-session-value")).Text);
    }

    [Fact]
    public void StreamingSSR_PersistsSupplyParameterFromTempData_AfterAsyncRendering()
    {
        Navigate($"{ServerPathBase}/streaming-session-persistence");

        // Wait for streaming to complete
        Browser.Exists(By.Id("streaming-complete"));
        Browser.Equal("tempdata-set-during-streaming", () => Browser.FindElement(By.Id("tempdata-value")).Text);

        // Navigate to the read page to verify TempData was persisted via session storage
        Navigate($"{ServerPathBase}/streaming-session-persistence/read");
        Browser.Equal("tempdata-set-during-streaming", () => Browser.FindElement(By.Id("read-tempdata-supply-value")).Text);
    }

    [Fact]
    public void StreamingSSR_PersistsTempDataCascadingParameter_AfterAsyncRendering()
    {
        Navigate($"{ServerPathBase}/streaming-session-persistence");

        // Wait for streaming to complete
        Browser.Exists(By.Id("streaming-complete"));

        // Navigate to the read page to verify ITempData values set during streaming were persisted
        Navigate($"{ServerPathBase}/streaming-session-persistence/read");
        Browser.Equal("streaming-tempdata-message", () => Browser.FindElement(By.Id("read-tempdata-message")).Text);
    }
}
