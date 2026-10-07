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
    public SectionsWithErrorBoundaryTest
        (BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<BasicTestApp.SectionsTest.SectionsWithErrorBoundary>();
    }

    [Fact]
    public void RenderSectionContent_ErrorBoundaryForSectionOutletContentIsDeterminedByMatchingSectionContent()
    {
        // Doesn't matter if SectionOutlet is rendered before or after SectionContent
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.FindElement(By.Id("error-button")).Click();

        Browser.Equal("Sorry!", () => Browser.Exists(By.Id("error-content")).Text);
    }

    [Fact]
    public void RenderTwoSectionContentsWithSameId_ErrorBoundaryForSectionOutletIsDeterminedByLastRenderedSectionContent()
    {
        // show that after second sc error thrown then first is now rendered and is functional
        Browser.FindElement(By.Id("render-second-section-content")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-section-outlet")).Click();

        Browser.FindElement(By.Id("error-button")).Click();

        Browser.Exists(By.ClassName("blazor-error-boundary"));
    }

    [Fact]
    public void SecondSectionContentIdChanged_ErrorBoundaryForSectionOutletIsDeterminedByFirstSectionContent()
    {
        Browser.FindElement(By.Id("render-second-section-content")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-section-outlet")).Click();

        Browser.FindElement(By.Id("error-button")).Click();

        Browser.Exists(By.ClassName("blazor-error-boundary"));
    }

    [Fact]
    public void SecondSectionContentDisposed_ErrorBoundaryForSectionOutletIsDeterminedByFirstSectionContent()
    {
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();
        Browser.FindElement(By.Id("render-section-outlet")).Click();

        Browser.FindElement(By.Id("dispose-second-section-content")).Click();
        Browser.FindElement(By.Id("error-button")).Click();

        Browser.Exists(By.ClassName("blazor-error-boundary"));
    }

    [Fact]
    public void FirstSectionContentDisposedThenRenderSecondSectionContent_ErrorBoundaryForSectionOutletIsDeterminedBySecondSectionContent()
    {
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();

        Browser.FindElement(By.Id("dispose-first-section-content")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();
        Browser.FindElement(By.Id("error-button")).Click();

        Browser.Equal("Sorry!", () => Browser.Exists(By.Id("error-content")).Text);
    }

    [Fact]
    public void SectionOutletIdChanged_ErrorBoundaryForSectionOutletIsDeterminedByMatchingSectionContent()
    {
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("change-second-section-content-id")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.FindElement(By.Id("change-section-outlet-id")).Click();
        Browser.FindElement(By.Id("error-button")).Click();

        Browser.Equal("Sorry!", () => Browser.Exists(By.Id("error-content")).Text);
    }
}
