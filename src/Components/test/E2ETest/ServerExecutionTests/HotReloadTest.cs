// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using BasicTestApp.HotReload;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class HotReloadTest : ServerTestBase<BasicTestAppServerSiteFixture<HotReloadStartup>>
{
    public HotReloadTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<HotReloadStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync(Guid.NewGuid().ToString());
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<RenderOnHotReload>();
    }

    [Fact]
    public async Task InvokingRender_CausesComponentToRender()
    {
        Browser.Equal("This component was rendered 1 time(s).", () => Browser.Exists(By.TagName("h2")).Text);
        Browser.Equal("Initial title", () => Browser.Exists(By.TagName("h3")).Text);
        Browser.Equal("Component with ShouldRender=false was rendered 1 time(s).", () => Browser.Exists(By.TagName("h4")).Text);

        using var client = new HttpClient { BaseAddress = _serverFixture.RootUri };
        var response = await client.GetAsync("/rerender");
        response.EnsureSuccessStatusCode();

        Browser.Equal("This component was rendered 2 time(s).", () => Browser.Exists(By.TagName("h2")).Text);
        Browser.Equal("Initial title", () => Browser.Exists(By.TagName("h3")).Text);
        Browser.Equal("Component with ShouldRender=false was rendered 2 time(s).", () => Browser.Exists(By.TagName("h4")).Text);
    }
}
