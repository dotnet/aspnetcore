// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Components.E2ETest;
using OpenQA.Selenium;
using Microsoft.AspNetCore.Components.Sections;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class SectionsWithErrorBoundaryTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    private IWebElement _appElement;

    public SectionsWithErrorBoundaryTest
        (BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        _appElement = Browser.MountTestComponent<BasicTestApp.SectionsTest.SectionsWithErrorBoundary>();
    }

    [Fact]
    public void ErrorBoundaryForSectionContentHandlesExceptionInSectionOutlet()
    {
        Browser.Equal("First Section", () => Browser.Exists(By.TagName("h1")).Text);

        _appElement.FindElement(By.Id("error-button")).Click();

        Browser.Exists(By.ClassName("blazor-error-boundary"));
    }

    [Fact]
    public void SectionContentChanged_ErrorBoundaryHandlesExceptionInSectionOutlet()
    {
        _appElement.FindElement(By.Id("change-section-content")).Click();

        Browser.Equal("Second Section", () => Browser.Exists(By.TagName("h1")).Text);

        _appElement.FindElement(By.Id("error-button")).Click();

        Browser.Equal("Sorry!", () => Browser.Exists(By.TagName("p")).Text);
    }
}
