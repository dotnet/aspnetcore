// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BasicTestApp.HotReload;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;
using System.Net.Http;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
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
            Navigate(ServerPathBase, noReload: false);
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
}
