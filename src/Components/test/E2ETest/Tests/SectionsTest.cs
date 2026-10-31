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
        Navigate(ServerPathBase);
        _appElement = Browser.MountTestComponent<BasicTestApp.SectionsTest.ParentComponentWithTwoChildren>();
    }

    [Fact]
    public void RenderTwoSectionOutletsWithSameSectionId_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-outlet-same-id")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains("There is already a subscriber to the content with the given section ID 'System.Object'", logs[0].Message);
    }

    [Fact]
    public void RenderTwoSectionOutletsWithSameSectionName_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-outlet-same-name")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains("There is already a subscriber to the content with the given section ID 'test1'", logs[0].Message);
    }

    [Fact]
    public void RenderTwoSectionOutletsWithEqualSectionNameToSectionId_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-outlet-equal-name-id")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains("There is already a subscriber to the content with the given section ID 'test2'", logs[0].Message);
    }

    [Fact]
    public void RenderSectionOutletWithSectionNameAndSectionId_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-outlet-with-name-id")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains($"{nameof(SectionOutlet)} requires that '{nameof(SectionOutlet.SectionName)}' and '{nameof(SectionOutlet.SectionId)}' cannot both have non-null values.", logs[0].Message);
    }

    [Fact]
    public void RenderSectionOutletWithoutSectionNameAndSectionId_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-outlet-without-name-id")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains($"{nameof(SectionOutlet)} requires a non-null value either for '{nameof(SectionOutlet.SectionName)}' or '{nameof(SectionOutlet.SectionId)}'.", logs[0].Message);
    }

    [Fact]
    public void RenderSectionContentWithSectionNameAndSectionId_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-content-with-name-id")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains($"{nameof(SectionContent)} requires that '{nameof(SectionContent.SectionName)}' and '{nameof(SectionContent.SectionId)}' cannot both have non-null values.", logs[0].Message);
    }

    [Fact]
    public void RenderSectionContentWithoutSectionNameAndSectionId_ThrowsException()
    {
        _appElement.FindElement(By.Id("section-content-without-name-id")).Click();

        var logs = Browser.GetBrowserLogs(LogLevel.Severe);

        Assert.True(logs.Count > 0);

        Assert.Contains($"{nameof(SectionContent)} requires a non-null value either for '{nameof(SectionContent.SectionName)}' or '{nameof(SectionContent.SectionId)}'.", logs[0].Message);
    }

    [Fact]
    public void NoExistingSectionContents_SectionOutletsRenderNothing()
    {
        // At the beginning no SectionContents are rendered
        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void RenderSectionContentWithSectionId_MatchingSectionOutletRendersContentSuccessfully()
    {
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();

        var counter = Browser.Exists(By.Id("counter"));
        Assert.Equal("0", counter.Text);

        _appElement.FindElement(By.Id("increment-button")).Click();

        Assert.Equal("1", counter.Text);
    }

    [Fact]
    public void RenderSectionContentWithSectionName_MatchingSectionOutletRendersContentSuccessfully()
    {
        _appElement.FindElement(By.Id("section-content-with-name")).Click();

        Browser.Exists(By.Id("test6"));
    }

    [Fact]
    public void SectionContentWithSectionNameGetsDisposed_OldSectionOutletNoLongerRendersContent()
    {
        _appElement.FindElement(By.Id("section-content-with-name")).Click();

        Browser.Exists(By.Id("test6"));

        _appElement.FindElement(By.Id("section-content-with-name-dispose")).Click();

        Browser.DoesNotExist(By.Id("test6"));
    }

    [Fact]
    public void SectionOutletWithSectionNameGetsDisposed_ContentDisappears()
    {
        // Render Counter and change its id so the content is rendered in second SectionOutlet
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-change-section-content-id")).Click();

        Browser.Exists(By.Id("counter"));

        _appElement.FindElement(By.Id("second-section-outlet-dispose")).Click();

        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void RenderTwoSectionContentsWithSameSectionId_LastRenderedOverridesSectionOutletContent()
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
    public void SectionContentSectionIdChanges_MatchingSectionOutletWithSectionNameRendersContent()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-change-section-content-id")).Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void SectionContentIdChangesToNonExisting_NoMatchingSectionOutletResultingNoRendering()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-change-section-content-id-nonexisting")).Click();

        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SectionContentSectionNameChanges_MatchingSectionOutletWithSectionIdRendersContent()
    {
        _appElement.FindElement(By.Id("section-content-with-name")).Click();

        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("counter-change-section-content-id")).Click();

        // Counter Component Content overrides second SectionContent
        Browser.DoesNotExist(By.Id("test6"));
        Browser.Exists(By.Id("counter"));

        _appElement.FindElement(By.Id("section-content-with-name-change-name")).Click();

        Browser.Exists(By.Id("test6"));
        Browser.Exists(By.Id("counter"));
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
