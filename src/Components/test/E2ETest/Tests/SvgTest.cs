// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class SvgTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public SvgTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
    }

    [Fact]
    public void CanRenderSvgWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-callback"));
        Assert.NotNull(svgElement);

        var svgCircleElement = svgElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
        Assert.NotNull(svgCircleElement);
        Assert.Equal("10", svgCircleElement.GetAttribute("r"));

        appElement.FindElement(By.TagName("button")).Click();
        Browser.Equal("20", () => svgCircleElement.GetAttribute("r"));
    }

    [Fact]
    public void CanRenderSvgWithAttributeRemoval()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-callback"));
        Assert.NotNull(svgElement);

        var svgCircleElement = svgElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
        Assert.NotNull(svgCircleElement);
        Assert.Equal("stroke: red;", svgCircleElement.GetAttribute("style"));

        appElement.FindElement(By.TagName("button")).Click();
        Browser.Equal("", () => svgCircleElement.GetAttribute("style"));
    }

    [Fact]
    public void CanRenderSvgChildComponentWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-child-component"));
        Assert.NotNull(svgElement);

        var svgCircleElement = svgElement.FindElement(By.XPath("//*[local-name()='circle' and namespace-uri()='http://www.w3.org/2000/svg']"));
        Assert.NotNull(svgCircleElement);
    }

    [Fact]
    public void CanRenderVariablesInForeignObject()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-foreign-object"));
        Assert.NotNull(svgElement);

        Func<IEnumerable<IWebElement>> strongElement =
            () => svgElement.FindElements(By.TagName("strong"));

        Browser.Collection<IWebElement>(strongElement,
            e => Assert.Equal("thestringfoo", e.Text),
            e => Assert.Equal("thestringbar", e.Text));
    }

    [Fact]
    public void CanRenderSvgWithLink()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-link"));
        Assert.NotNull(svgElement);

        var svgLinkElement = svgElement.FindElement(By.Id("navlink-in-svg"));
        Assert.NotNull(svgLinkElement);
        svgLinkElement.Click();

        var currentScenario = Browser.FindElement(By.Id("test-selector-select"));
        // Should have navigated away from current page
        Browser.True(() => Browser.Url.EndsWith("/subdir/counter", StringComparison.Ordinal));
    }

    [Fact]
    public void CanRenderSvgWithTwoWayBinding()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-two-way-binding"));
        Assert.NotNull(svgElement);

        var valueElement = appElement.FindElement(By.Id("svg-with-two-way-binding-value"));
        Assert.Equal("10", valueElement.Text);

        var svgInputElement = svgElement.FindElement(By.TagName("input"));
        Assert.NotNull(svgInputElement);

        svgInputElement.SendKeys(Keys.Backspace);
        svgInputElement.SendKeys(Keys.Backspace);
        svgInputElement.SendKeys("15");
        Assert.Equal("15", valueElement.Text);
    }

    [Fact]
    public void CanRenderSvgRenderFragment()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-render-fragment"));
        Assert.NotNull(svgElement);

        var svgForeignObjectElement = svgElement.FindElement(By.TagName("foreignObject"));
        Assert.NotNull(svgForeignObjectElement);

        Assert.Contains("Hello", svgForeignObjectElement.Text);
    }

    [Fact]
    public void CanRenderSvgWithScopedCSS()
    {
        var appElement = Browser.MountTestComponent<SvgComponent>();

        var svgElement = appElement.FindElement(By.Id("svg-with-css-scope"));
        Assert.NotNull(svgElement);

        var svgCircleElement = svgElement.FindElement(By.TagName("circle"));
        Assert.NotNull(svgCircleElement);

        Assert.Equal("rgb(0, 128, 0)", svgCircleElement.GetCssValue("fill"));

    }
}
