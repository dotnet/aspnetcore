// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

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

        Browser.Exists(By.Id("streaming-complete"));
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Equal("set-during-streaming", () => Browser.FindElement(By.Id("text-email")).Text);
    }

    [Fact]
    public void StreamingSSR_PersistsSupplyParameterFromTempData_AfterAsyncRendering()
    {
        Navigate($"{ServerPathBase}/streaming-session-persistence");

        Browser.Exists(By.Id("streaming-complete"));
        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("tempdata-set-during-streaming", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);
    }

    [Fact]
    public void StreamingSSR_PersistsTempDataCascadingParameter_AfterAsyncRendering()
    {
        Navigate($"{ServerPathBase}/streaming-session-persistence");

        Browser.Exists(By.Id("streaming-complete"));
        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("streaming-tempdata-message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void StreamingSSR_DeferredChildSubscription_DoesNotPersistSession_OnFirstRequest()
    {
        Navigate($"{ServerPathBase}/streaming-parent-with-deferred-child");

        Browser.Exists(By.Id("deferred-child-done"));
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Equal(string.Empty, () => Browser.FindElement(By.Id("text-email")).Text);
    }
}
