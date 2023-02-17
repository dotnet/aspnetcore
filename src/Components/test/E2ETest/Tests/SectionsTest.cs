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
        _appElement = Browser.MountTestComponent<BasicTestApp.SectionsTest.ParentComponent>();
    }

    [Fact]
    public void SectionOutletInParentComponentRendersSectionContentOfChildComponent()
    {
        //Nothing is chosen yet
        Assert.False(TryGetElementById(out _, "counter"));
        var options = _appElement.FindElement(By.Id("child-component"));

        // Choose Counter
        options.FindElement(By.Name("counter")).Click();
        Assert.True(TryGetElementById(out var counter, "counter"));

        Assert.Equal("0", counter.Text);
        var incrememntButton = _appElement.FindElement(By.Id("increment_button"));

        incrememntButton.Click();
        Assert.Equal("1", counter.Text);
    }

    [Fact]
    public void SectionOutletInParentComponentRendersSectionContentOfAnotherChildComponent()
    {
        var options = _appElement.FindElement(By.Id("child-component"));

        // Choose Counter
        options.FindElement(By.Name("counter")).Click();
        Assert.True(TryGetElementById(out _, "counter"));

        // Choose Simple Component
        options.FindElement(By.Name("simple-component")).Click();
        Assert.True(TryGetElementById(out var simpleComponentText, "text"));
        Assert.Equal("Hello!", simpleComponentText.Text);
        Assert.False(TryGetElementById(out _, "counter"));
    }

    private bool TryGetElementById(out IWebElement counter, string id)
    {
        try
        {
            counter = _appElement.FindElement(By.Id(id));
            return true;
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            counter = null;
            return false;
        }
    }
}
