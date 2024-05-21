// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class WebAssemblyPrerenderedTest : ServerTestBase<AspNetSiteServerFixture>
{
    public WebAssemblyPrerenderedTest(
        BrowserFixture browserFixture,
        AspNetSiteServerFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.BuildWebHostMethod = Wasm.Prerendered.Server.Program.BuildWebHost;
        serverFixture.Environment = AspNetEnvironment.Development;

        var testTrimmedApps = typeof(ToggleExecutionModeServerFixture<>).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedOrMultithreadingApps")
            .Value == "true";

        if (testTrimmedApps)
        {
            serverFixture.GetContentRootMethod = GetPublishedContentRoot;
        }
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

    private static string GetPublishedContentRoot(Assembly assembly)
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, "trimmed-or-threading", assembly.GetName().Name);

        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Test is configured to use trimmed outputs, but trimmed outputs were not found in {contentRoot}.");
        }

        return contentRoot;
    }
}
