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

    [Fact]
    public void InitializersWork()
    {
        Browser.Exists(By.Id("initializer-start"));
        Browser.Exists(By.Id("initializer-end"));
    }

    [Fact]
    public void CanLoadJsModulePackagesFromLibrary()
    {
        Browser.MountTestComponent<ExternalContentPackage>();
        Browser.Equal<string>("Hello from module", () => Browser.Exists(By.CssSelector(".js-module-message > p")).Text);
    }
}
