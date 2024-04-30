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

public class SectionsWithCascadingParametersTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public SectionsWithCascadingParametersTest
        (BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<BasicTestApp.SectionsTest.SectionsWithCascadingParameters>();
    }

    [Fact]
    public void RenderSectionContent_CascadingParameterForSectionOutletIsDeterminedByMatchingSectionContent()
    {
        // Doesn't matter if SectionOutlet is rendered before or after SectionContent
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.Equal("Second Section with additional text for second section", () => Browser.Exists(By.TagName("p")).Text);
    }

    [Fact]
    public void ChangeCascadingValueForSectionContent_CascadingValueForSectionOutletIsDeterminedByMatchingSectionContent()
    {
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-section-outlet")).Click();

        Browser.FindElement(By.Id("change-cascading-value")).Click();

        Browser.Equal("First Section with additional text for second section", () => Browser.Exists(By.TagName("p")).Text);
    }

    [Fact]
    public void RenderTwoSectionContentsWithSameId_CascadingParameterForSectionOutletIsDeterminedByLastRenderedSectionContent()
    {
        Browser.FindElement(By.Id("render-second-section-content")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-section-outlet")).Click();

        Browser.Equal("First Section with additional text for first section", () => Browser.Exists(By.TagName("p")).Text);
    }

    [Fact]
    public void SecondSectionContentIdChanged_CascadingParameterForSectionOutletIsDeterminedByFirstSectionContent()
    {
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.FindElement(By.Id("change-second-section-content-id")).Click();

        Browser.Equal("First Section with additional text for first section", () => Browser.Exists(By.TagName("p")).Text);
    }

    [Fact]
    public void SecondSectionContentDisposed_CascadingParameterForSectionOutletIsDeterminedByFirstSectionContent()
    {
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();
        Browser.FindElement(By.Id("render-section-outlet")).Click();

        Browser.FindElement(By.Id("dispose-second-section-content")).Click();

        Browser.Equal("First Section with additional text for first section", () => Browser.Exists(By.TagName("p")).Text);
    }

    [Fact]
    public void FirstSectionContentDisposedThenRenderSecondSectionContent_CascadingParameterForSectionOutletIsDeterminedBySecondSectionContent()
    {
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();

        Browser.FindElement(By.Id("dispose-first-section-content")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.Equal("Second Section with additional text for second section", () => Browser.Exists(By.TagName("p")).Text);
    }

    [Fact]
    public void SectionOutletIdChanged_CascadingParameterForSectionOutletIsDeterminedByMatchingSectionContent()
    {
        Browser.FindElement(By.Id("render-section-outlet")).Click();
        Browser.FindElement(By.Id("render-first-section-content")).Click();
        Browser.FindElement(By.Id("change-second-section-content-id")).Click();
        Browser.FindElement(By.Id("render-second-section-content")).Click();

        Browser.FindElement(By.Id("change-section-outlet-id")).Click();

        Browser.Equal("Second Section with additional text for second section", () => Browser.Exists(By.TagName("p")).Text);
    }
}
