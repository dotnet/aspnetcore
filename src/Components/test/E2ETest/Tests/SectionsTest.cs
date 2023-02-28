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
        //Render Counter and TextComponent SectionContents with same Name
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

    [Fact]
    public void DefaultSectionContent_DoesNotOverrideAnotherSectionContent()
    {
        _appElement.FindElement(By.Id("text-section-content-make-default")).Click();

        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        // TextComponent SectionContent IsDefaultContent=true does not override Counter SectionContent
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void DefaultSectionContent_RendersWhenAnotherSectionContentGetsDisposed()
    {
        _appElement.FindElement(By.Id("text-section-content-make-default")).Click();

        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("counter-dispose-section-content")).Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.Exists(By.Id("text"));
    }

    [Fact]
    public void IsDefaultContentChanges_DoesNotOverrideAnotherSectionContent()
    {
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();
        _appElement.FindElement(By.Id("text-render-section-content")).Click();

        _appElement.FindElement(By.Id("text-section-content-make-default")).Click();

        // TextComponent SectionContent IsDefaultContent=true does not override Counter SectionContent
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void BothDefaultSectionContents_LastRenderedIsMoreDefault()
    {
        // Order of default doesn't matter before rendering
        _appElement.FindElement(By.Id("text-section-content-make-default")).Click();
        _appElement.FindElement(By.Id("counter-section-content-make-default")).Click();

        // Counter SectionContent rendered last so it is more "default" than TextComponent
        _appElement.FindElement(By.Id("text-render-section-content")).Click();
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();

        Browser.Exists(By.Id("text"));
        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void BothDefaultSectionContents_LastRenderedChanges_FirstRenderedIsNowDefault()
    {
        // Order of default doesn't matter before rendering
        _appElement.FindElement(By.Id("text-section-content-make-default")).Click();
        _appElement.FindElement(By.Id("counter-section-content-make-default")).Click();

        // Counter SectionContent rendered last so it is more "default" than TextComponent
        _appElement.FindElement(By.Id("text-render-section-content")).Click();
        _appElement.FindElement(By.Id("counter-render-section-content")).Click();

        // Change Counter SectionContent to non default
        _appElement.FindElement(By.Id("counter-section-content-make-non-default")).Click();

        // TextComponent SectionContent is default
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }
}
