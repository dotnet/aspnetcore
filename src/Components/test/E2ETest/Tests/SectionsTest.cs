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
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();

        var counter = Browser.Exists(By.Id("counter"));
        Assert.Equal("0", counter.Text);

        _appElement.FindElement(By.Id("increment_button")).Click();

        Assert.Equal("1", counter.Text);
    }

    [Fact]
    public void RenderTwoSectionContentsWithSameName_LastRenderedOverridesSectionOutletContent()
    {
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();

        Browser.Exists(By.Id("counter"));

        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        Browser.Exists(By.Id("text"));
        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SecondSectionContentGetsDisposed_SectionOutletRendersFirstSectionContent()
    {
        //Render Counter and TextComponent SectionContents with same Name
        // TextComponent's SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        _appElement.FindElement(By.Id("text_dispose_section_content")).Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void BothSectionContentsGetDisposed_SectionOutletsRenderNothing()
    {
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        _appElement.FindElement(By.Id("counter_dispose_section_content")).Click();
        _appElement.FindElement(By.Id("text_dispose_section_content")).Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void SectionContentNameChanges_MatchingSectionOutletRendersContent()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent's SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        _appElement.FindElement(By.Id("counter_change_section_content_name")).Click();

        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void SectionContentNameChangesToNonExisting_NoMatchingSectionOutletResultingNoRendering()
    {
        // Render Counter and TextComponent SectionContents with same Name
        // TextComponent's SectionContent overrides Counter SectionContent
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        _appElement.FindElement(By.Id("counter_change_section_content_name_nonexisting")).Click();

        Browser.DoesNotExist(By.Id("counter"));
    }

    [Fact]
    public void SectionOutletGetsDisposed_NoContentsRendered()
    {
        // Render Counter and TextComponent SectionContents with same Name      
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        // TextComponent's SectionContent overrides Counter SectionContent
        Browser.Exists(By.Id("text"));

        _appElement.FindElement(By.Id("section_outlet_dispose")).Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.DoesNotExist(By.Id("text"));
    }

    [Fact]
    public void DefaultSectionContent_DoesNotOverrideAnotherSectionContent()
    {
        _appElement.FindElement(By.Id("text_section_content_make_default")).Click();

        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        // TextComponent's SectionContent IsDefaultContent=true does not override Counter's SectionContent
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }

    [Fact]
    public void BothDefaultSectionContents_()
    {
        //TODO
    }

    [Fact]
    public void DefaultSectionContent_RendersWhenAnotherSectionContentGetsDisposed()
    {
        _appElement.FindElement(By.Id("text_section_content_make_default")).Click();

        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        _appElement.FindElement(By.Id("counter_dispose_section_content")).Click();

        Browser.DoesNotExist(By.Id("counter"));
        Browser.Exists(By.Id("text"));
    }

    // TODO support changing IsDefaultContent
    [Fact]
    public void IsDefaultContentChanges_DoesNotOverrideAnotherSectionContent()
    {
        _appElement.FindElement(By.Id("counter_render_section_content")).Click();
        _appElement.FindElement(By.Id("text_render_section_content")).Click();

        _appElement.FindElement(By.Id("text_section_content_make_default")).Click();

        // TextComponent's SectionContent IsDefaultContent=true does not override Counter's SectionContent
        Browser.DoesNotExist(By.Id("text"));
        Browser.Exists(By.Id("counter"));
    }
}
