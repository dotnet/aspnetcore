// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyPrerenderedTest : ServerTestBase<TrimmingServerFixture<Wasm.Prerendered.Server.Startup>>
{
    public WebAssemblyPrerenderedTest(
        BrowserFixture browserFixture,
        TrimmingServerFixture<Wasm.Prerendered.Server.Startup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.Environment = AspNetEnvironment.Development;
    }

    [Fact]
    public void CanPrerenderAndAddHeadOutletRootComponent()
    {
        Navigate("/");

        // Verify that the title is updated during prerendering
        Browser.Equal("Current count: 0", () => Browser.Title);
        Browser.Click(By.Id("start-blazor"));

        WaitUntilLoaded();

        // Verify that the HeadOutlet root component was added after prerendering
        Browser.Click(By.Id("increment-count"));
        Browser.Equal("Current count: 1", () => Browser.Title);
    }

    private void WaitUntilLoaded()
    {
        var jsExecutor = (IJavaScriptExecutor)Browser;
        Browser.True(() => jsExecutor.ExecuteScript("return window['__aspnetcore__testing__blazor_wasm__started__'];") is not null);
    }
}
