// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Components.E2ETest;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class SectionsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    private IWebElement _appElement;

    public SectionsTest
        (BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        _appElement = Browser.MountTestComponent<BasicTestApp.SectionsTest.ParentComponentWithTwoChildren>();
    }

    [Fact]
    public void NoExistingSectionContents_SectionOutletsRenderNothing()
    {
        // At the beginning no SectionContents are rendered
        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void RenderOneSectionContent_MatchingSectionOutletRendersContentSuccessfully()
    {
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();

        var counter = Browser.Exists(By.Id("counter"));
        Assert.Equal("0", counter.Text);

        _appElement.FindElement(By.Id("increment-button")).Click();

        Assert.Equal("1", counter.Text);
    }

    [Fact]
    public void RenderTwoSectionContentsWithSameId_LastRenderedOverridesSectionOutletContent()
    {
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();

        Browser.Exists(By.Id("counter"));

        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        Browser.Exists(By.Id("text"));
        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SecondSectionContentGetsDisposed_SectionOutletRendersFirstSectionContent()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("text-dispose-section-content")).Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void BothSectionContentsGetDisposed_SectionOutletsRenderNothing()
    {
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-dispose-section-content")).Click();
        _appElement.FindElement(By.Id("text-dispose-section-content")).Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void SectionContentIdChanges_MatchingSectionOutletRendersContent()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-change-section-content-name")).Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void SectionContentIdChangesToNonExisting_NoMatchingSectionOutletResultingNoRendering()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-change-section-content-name-nonexisting")).Click();

        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SectionOutletGetsDisposed_NoContentsRendered()
    {
        // Render Counter and TextComponent SectionContents with same Name      
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        // TextComponent SectionContent overrides Counter SectionContent
        Browser.Exists(By.Id("text"));

        _appElement.FindElement(By.Id("section-outlet-dispose")).Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }
}
