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

    private IWebElement _renderCounterSectionContent;
    private IWebElement _renderTextSectionContent;

    private IWebElement _disposeCounterSectionContent;
    private IWebElement _disposeTextSectionContent;

    private IWebElement _changeCounterSectionContentName;
    private IWebElement _changeCounterSectionContentNameToNonExisting;

    private IWebElement _makeCounterSectionContentDefault;
    private IWebElement _makeTextSectionContentDefault;
    private IWebElement _makeCounterSectionContentNonDefault;

    private IWebElement _disposeSectionOutlet;

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

        _renderCounterSectionContent = _appElement.FindElement(By.Id("counter-render-section-content"));
        _renderTextSectionContent = _appElement.FindElement(By.Id("text-render-section-content"));

        _disposeCounterSectionContent = _appElement.FindElement(By.Id("counter-dispose-section-content"));
        _disposeTextSectionContent = _appElement.FindElement(By.Id("text-dispose-section-content"));

        _changeCounterSectionContentName = _appElement.FindElement(By.Id("counter-change-section-content-name"));
        _changeCounterSectionContentNameToNonExisting = _appElement.FindElement(By.Id("counter-change-section-content-name-nonexisting"));

        _makeCounterSectionContentDefault = _appElement.FindElement(By.Id("counter-section-content-make-default"));
        _makeTextSectionContentDefault = _appElement.FindElement(By.Id("text-section-content-make-default"));
        _makeCounterSectionContentNonDefault = _appElement.FindElement(By.Id("counter-section-content-make-non-default"));

        _disposeSectionOutlet = _appElement.FindElement(By.Id("section-outlet-dispose"));
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
        _renderCounterSectionContent.Click();

        var counter = Browser.Exists(By.Id("counter"));
        Assert.Equal("0", counter.Text);

        _appElement.FindElement(By.Id("increment-button")).Click();

        Assert.Equal("1", counter.Text);
    }

    [Fact]
    public void RenderTwoSectionContentsWithSameName_LastRenderedOverridesSectionOutletContent()
    {
        _renderCounterSectionContent.Click();

        Browser.Exists(By.Id("counter"));

        _renderTextSectionContent.Click();

        Browser.Exists(By.Id("text"));
        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SecondSectionContentGetsDisposed_SectionOutletRendersFirstSectionContent()
    {
        //Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        _disposeTextSectionContent.Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void BothSectionContentsGetDisposed_SectionOutletsRenderNothing()
    {
        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        _disposeCounterSectionContent.Click();
        _disposeTextSectionContent.Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void SectionContentNameChanges_MatchingSectionOutletRendersContent()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        _changeCounterSectionContentName.Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void SectionContentNameChangesToNonExisting_NoMatchingSectionOutletResultingNoRendering()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent SectionContent overrides Counter SectionContent
        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        _changeCounterSectionContentNameToNonExisting.Click();

        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SectionOutletGetsDisposed_NoContentsRendered()
    {
        // Render Counter and TextComponent SectionContents with same Name      
        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        // TextComponent SectionContent overrides Counter SectionContent
        Browser.Exists(By.Id("text"));

        _disposeSectionOutlet.Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void DefaultSectionContent_DoesNotOverrideAnotherSectionContent()
    {
        _makeTextSectionContentDefault.Click();

        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        // TextComponent SectionContent IsDefaultContent=true does not override Counter SectionContent
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void DefaultSectionContent_RendersWhenAnotherSectionContentGetsDisposed()
    {
        _makeTextSectionContentDefault.Click();

        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        _disposeCounterSectionContent.Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.Exists(By.Id("text"));
    }

    [Fact]
    public void IsDefaultContentChanges_DoesNotOverrideAnotherSectionContent()
    {
        _renderCounterSectionContent.Click();
        _renderTextSectionContent.Click();

        _makeTextSectionContentDefault.Click();

        // TextComponent SectionContent IsDefaultContent=true does not override Counter SectionContent
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void BothDefaultSectionContents_LastRenderedIsMoreDefault()
    {
        // Order of default doesn't matter before rendering
        _makeTextSectionContentDefault.Click();
        _makeCounterSectionContentDefault.Click();

        // Counter SectionContent rendered last so it is more "default" than TextComponent
        _renderTextSectionContent.Click();
        _renderCounterSectionContent.Click();

        Browser.Exists(By.Id("text"));
        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void BothDefaultSectionContents_LastRenderedChanges_FirstRenderedIsNowDefault()
    {
        // Order of default doesn't matter before rendering
        _makeTextSectionContentDefault.Click();
        _makeCounterSectionContentDefault.Click();

        // Counter SectionContent rendered last so it is more "default" than TextComponent
        _renderTextSectionContent.Click();
        _renderCounterSectionContent.Click();

        // Change Counter SectionContent to non default
        _makeCounterSectionContentNonDefault.Click();

        // TextComponent SectionContent is default
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }
}
