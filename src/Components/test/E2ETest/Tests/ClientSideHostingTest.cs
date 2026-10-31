// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

/// <summary>
/// Tests for various MapFallbackToClientSideBlazor overloads. We're just verifying that things render correctly.
/// That means that routing and file serving is working for the startup pattern under test.
/// </summary>
public class ClientSideHostingTest :
    ServerTestBase<BasicTestAppServerSiteFixture<TestServer.StartupWithMapFallbackToClientSideBlazor>>
{
    public ClientSideHostingTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<TestServer.StartupWithMapFallbackToClientSideBlazor> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void MapFallbackToClientSideBlazor_FilePath()
    {
        Navigate("/subdir/filepath");
        WaitUntilLoaded();
        Assert.NotNull(Browser.Exists(By.Id("test-selector")));
    }

    [Fact]
    public void MapFallbackToClientSideBlazor_Pattern_FilePath()
    {
        Navigate("/subdir/pattern_filepath/test");
        WaitUntilLoaded();
        Assert.NotNull(Browser.Exists(By.Id("test-selector")));
    }

    [Fact]
    public void MapFallbackToClientSideBlazor_AssemblyPath_FilePath()
    {
        Navigate("/subdir/assemblypath_filepath");
        WaitUntilLoaded();
        Assert.NotNull(Browser.Exists(By.Id("test-selector")));
    }

    [Fact]
    public void MapFallbackToClientSideBlazor_AssemblyPath_Pattern_FilePath()
    {
        Navigate("/subdir/assemblypath_pattern_filepath/test");
        WaitUntilLoaded();
        Assert.NotNull(Browser.Exists(By.Id("test-selector")));
    }

    private void WaitUntilLoaded()
    {
        var app = Browser.Exists(By.TagName("app"));
        Browser.NotEqual("Loading...", () => app.Text);
    }
}
