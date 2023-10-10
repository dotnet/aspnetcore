// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class JsInitializersTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public JsInitializersTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase + "#initializer");
    }

    // This case is essentially the same as Blazor.web.js with 'legacy' initializers enabled by default, as Blazor.server.js and Blazor.Webassembly.js
    // both need to support the legacy initializers for backwards compatibility.
    // The implementation accounts for the new 'modern' initializers and prefers those over the 'legacy' ones.
    [Fact]
    public void InitializersWork()
    {
        Browser.Exists(By.Id("initializer-start"));
        Browser.Exists(By.Id("initializer-end"));
        var expectedCallbacks = GetExpectedCallbacks();
        Browser.Equal(expectedCallbacks.Length, () => Browser.FindElements(By.CssSelector("#initializers-content > p")).Count);
        foreach (var callback in expectedCallbacks)
        {
            Browser.Exists(By.Id(callback));
        }
    }

    protected virtual string[] GetExpectedCallbacks()
    {
        return ["classic-before-start",
            "classic-after-started",
            "classic-and-modern-before-web-assembly-start",
            "classic-and-modern-after-web-assembly-started",
            "modern-before-web-assembly-start",
            "modern-after-web-assembly-started"];
    }

    [Fact]
    public void CanLoadJsModulePackagesFromLibrary()
    {
        Browser.MountTestComponent<ExternalContentPackage>();
        Browser.Equal<string>("Hello from module", () => Browser.Exists(By.CssSelector(".js-module-message > p")).Text);
    }
}
