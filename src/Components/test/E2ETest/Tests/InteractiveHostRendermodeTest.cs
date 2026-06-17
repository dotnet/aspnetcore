// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class InteractiveHostRendermodeTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public InteractiveHostRendermodeTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    [InlineData("auto")]
    [InlineData("static")]
    public void EmbeddingServerAppInsideIframe_Works(string renderMode)
    {
        Navigate($"/subdir/ComponentPlatform?suppress-autostart&ComponentRenderMode={renderMode}");

        Browser.Equal(renderMode, () => Browser.Exists(By.Id("host-render-mode")).Text);
        Browser.Equal("False", () => Browser.Exists(By.Id("platform-is-interactive")).Text);

        Browser.Click(By.Id("call-blazor-start"));

        if (renderMode == "static")
        {
            Browser.Equal("False", () => Browser.Exists(By.Id("platform-is-interactive")).Text);
        }
        else
        {
            Browser.Equal("True", () => Browser.Exists(By.Id("platform-is-interactive")).Text);
        }

        if (renderMode != "auto")
        {
            Browser.Equal(renderMode, () => Browser.Exists(By.Id("host-render-mode")).Text);
        }
        else
        {
            Browser.True(() => Browser.Exists(By.Id("host-render-mode")).Text is "server" or "webassembly");
        }
    }
}

