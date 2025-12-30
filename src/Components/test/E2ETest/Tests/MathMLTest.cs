// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class MathMLTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public MathMLTest(
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
    public void CanRenderMathMLWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<MathMLComponent>();

        var mathElement = appElement.FindElement(By.Id("mathml-with-callback"));
        Assert.NotNull(mathElement);

        // Verify the math element has the correct MathML namespace
        var mathMrowElement = mathElement.FindElement(By.XPath(".//*[local-name()='mrow' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(mathMrowElement);

        // Verify child elements also have the correct namespace
        var mathMnElement = mathElement.FindElement(By.XPath(".//*[local-name()='mn' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(mathMnElement);
        Assert.Equal("10", mathMnElement.Text);

        // Click button to update and verify the value changes while maintaining correct namespace
        appElement.FindElement(By.Id("increment-btn")).Click();
        Browser.Equal("11", () => mathMnElement.Text);
    }

    [Fact]
    public void CanRenderStaticMathMLWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<MathMLComponent>();

        var mathElement = appElement.FindElement(By.Id("mathml-static"));
        Assert.NotNull(mathElement);

        // Verify msup elements have the correct namespace
        var msupElements = mathElement.FindElements(By.XPath(".//*[local-name()='msup' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.Equal(3, msupElements.Count);
    }

    [Fact]
    public void CanRenderMathMLChildComponentWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<MathMLComponent>();

        var mathElement = appElement.FindElement(By.Id("mathml-with-child-component"));
        Assert.NotNull(mathElement);

        // The child component should render mrow with correct namespace
        var mathMrowElement = mathElement.FindElement(By.XPath(".//*[local-name()='mrow' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(mathMrowElement);

        // Verify mi element from child component
        var mathMiElement = mathElement.FindElement(By.XPath(".//*[local-name()='mi' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(mathMiElement);
        Assert.Equal("z", mathMiElement.Text);
    }

    [Fact]
    public void CanRenderConditionalMathMLWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<MathMLComponent>();

        // Initially the conditional MathML should not be present
        var conditionalMath = appElement.FindElements(By.Id("mathml-conditional"));
        Assert.Empty(conditionalMath);

        // Click toggle to show the conditional MathML
        appElement.FindElement(By.Id("toggle-btn")).Click();

        // Now the MathML should be present with correct namespace
        Browser.Exists(By.Id("mathml-conditional"));
        var mathElement = appElement.FindElement(By.Id("mathml-conditional"));

        var mathMrowElement = mathElement.FindElement(By.XPath(".//*[local-name()='mrow' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(mathMrowElement);
    }

    [Fact]
    public void CanRenderComplexMathMLWithCorrectNamespace()
    {
        var appElement = Browser.MountTestComponent<MathMLComponent>();

        var mathElement = appElement.FindElement(By.Id("mathml-complex"));
        Assert.NotNull(mathElement);

        // Verify mfrac element has the correct namespace
        var mfracElement = mathElement.FindElement(By.XPath(".//*[local-name()='mfrac' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(mfracElement);

        // Verify msqrt element has the correct namespace
        var msqrtElement = mathElement.FindElement(By.XPath(".//*[local-name()='msqrt' and namespace-uri()='http://www.w3.org/1998/Math/MathML']"));
        Assert.NotNull(msqrtElement);
    }
}
