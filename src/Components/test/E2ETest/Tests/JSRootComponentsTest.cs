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

public class JSRootComponentsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    protected IWebElement app;

    public JSRootComponentsTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        app = Browser.MountTestComponent<JavaScriptRootComponents>();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CanAddAndDisposeRootComponents(bool intoBlazorUi, bool attachShadowRoot)
    {
        var message = app.FindElement(By.Id("message"));

        if (attachShadowRoot)
        {
            app.FindElement(By.Id("add-shadow-root")).Click();
        }

        // We can add root components with initial parameters
        var buttonId = intoBlazorUi ? "add-root-component-inside-blazor" : "add-root-component";
        app.FindElement(By.Id(buttonId)).Click();

        // They render and work
        var containerId = intoBlazorUi ? "container-rendered-by-blazor" : "root-container-1";
        ISearchContext dynamicRootContainer = Browser.FindElement(By.Id(containerId));
        if (attachShadowRoot)
        {
            dynamicRootContainer = GetShadowRoot(dynamicRootContainer);
        }
        Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

        // We can dispose the root component
        app.FindElement(By.Id("remove-root-component")).Click();
        Browser.Equal($"Disposed component in {(attachShadowRoot ? "ShadowDOM" : containerId)}", () => message.Text);

        // It's gone from the UI
        Browser.Empty(() => dynamicRootContainer.FindElements(By.CssSelector("*")));

        AssertGlobalErrorState(false);
    }

    [Fact]
    public void CanAddAndRemoveMultipleRootComponentsToTheSameElement()
    {
        // Add, remove, re-add, all to the same element
        app.FindElement(By.Id("add-root-component-inside-blazor")).Click();
        app.FindElement(By.Id("remove-root-component")).Click();
        app.FindElement(By.Id("add-root-component-inside-blazor")).Click();

        // It functions
        var dynamicRootContainer = Browser.FindElement(By.Id("container-rendered-by-blazor"));
        Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

        AssertGlobalErrorState(false);
    }

    [Fact]
    public void CanUpdateParameters()
    {
        // Create the initial component
        app.FindElement(By.Id("add-root-component")).Click();
        var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
        var incrementButton = dynamicRootContainer.FindElement(By.ClassName("increment"));
        var clickCount = dynamicRootContainer.FindElement(By.ClassName("click-count"));
        incrementButton.Click();
        Browser.Equal("1", () => clickCount.Text);

        // Supply updated parameters
        var incrementAmount = app.FindElement(By.Id("increment-amount"));
        incrementAmount.Clear();
        incrementAmount.SendKeys("4");
        app.FindElement(By.Id("set-increment-amount")).Click();
        incrementButton.Click();
        Browser.Equal("5", () => clickCount.Text);
    }

    [Fact]
    public void CanSupplyComplexParameters()
    {
        app.FindElement(By.Id("add-root-component")).Click();
        app.FindElement(By.Id("set-complex-params")).Click();

        var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
        Browser.Equal("123", () => dynamicRootContainer.FindElement(By.ClassName("increment-amount-value")).Text);
        Browser.Equal("Person is Bert, age 123.456", () => dynamicRootContainer.FindElement(By.ClassName("person-info")).Text);
        Browser.Equal("Value from JS object reference: You've added 1 components.", () => dynamicRootContainer.FindElement(By.ClassName("value-from-js")).Text);
        Browser.Equal("Value from .NET object reference: This is correct", () => dynamicRootContainer.FindElement(By.ClassName("value-from-dotnetobject")).Text);
        Browser.Equal("Byte array value: 2,3,5,7,11,13,17", () => dynamicRootContainer.FindElement(By.ClassName("value-from-bytearray")).Text);
    }

    [Fact]
    public void CanSupplyParametersIncrementally()
    {
        app.FindElement(By.Id("add-root-component")).Click();
        app.FindElement(By.Id("set-complex-params")).Click();

        var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
        Browser.Equal("123", () => dynamicRootContainer.FindElement(By.ClassName("increment-amount-value")).Text);

        // Supply updated parameters
        app.FindElement(By.Id("set-increment-amount")).Click();

        // This parameter was provided explicitly
        Browser.Equal("1", () => dynamicRootContainer.FindElement(By.ClassName("increment-amount-value")).Text);

        // ... but this one remains from before
        Browser.Equal("Person is Bert, age 123.456", () => dynamicRootContainer.FindElement(By.ClassName("person-info")).Text);
    }

    [Fact]
    public void SetParametersThrowsIfParametersAreInvalid()
    {
        app.FindElement(By.Id("add-root-component")).Click();
        app.FindElement(By.Id("set-invalid-params")).Click();
        Browser.Contains("Error setting parameters", () => app.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void CanSupplyCatchAllParameters()
    {
        app.FindElement(By.Id("add-root-component")).Click();
        app.FindElement(By.Id("set-catchall-params")).Click();
        Browser.Equal("Finished setting catchall parameters on component in root-container-1", () => Browser.FindElement(By.Id("message")).Text);

        var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
        var catchAllParams = dynamicRootContainer.FindElements(By.ClassName("unmatched-value"));
        Assert.Collection(catchAllParams,
            param =>
            {
                Assert.Equal("stringVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                Assert.Equal("String", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                Assert.Equal("Hello", param.FindElement(By.ClassName("unmatched-value-value")).Text);
            },
            param =>
            {
                Assert.Equal("wholeNumberVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                Assert.Equal("Double", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                Assert.Equal("1", param.FindElement(By.ClassName("unmatched-value-value")).Text);
            },
            param =>
            {
                Assert.Equal("fractionalNumberVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                Assert.Equal("Double", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                Assert.Equal("-123.456", param.FindElement(By.ClassName("unmatched-value-value")).Text);
            },
            param =>
            {
                Assert.Equal("trueVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                Assert.Equal("Boolean", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                Assert.Equal("True", param.FindElement(By.ClassName("unmatched-value-value")).Text);
            },
            param =>
            {
                Assert.Equal("falseVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                Assert.Equal("Boolean", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                Assert.Equal("False", param.FindElement(By.ClassName("unmatched-value-value")).Text);
            },
            param =>
            {
                Assert.Equal("nullVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                Assert.Equal("null", param.FindElement(By.ClassName("unmatched-value-type")).Text);
            });
    }

    [Fact]
    public void CanSupplyAndInvokeFunctionParameters()
    {
        var containerId = "root-container-1";

        app.FindElement(By.Id("add-root-component")).Click();
        app.FindElement(By.Id("set-callback-params")).Click();
        Browser.Equal($"Finished setting callback parameters on component in {containerId}", () => app.FindElement(By.Id("message")).Text);

        var container = Browser.FindElement(By.Id(containerId));

        // Invoke the callback without params.
        container.FindElement(By.ClassName("js-callback")).Click();
        Browser.Equal($"JavaScript button callback invoked (id=0)", () => app.FindElement(By.Id("message")).Text);

        // Invoke the callback with params.
        container.FindElement(By.ClassName("js-callback-with-params")).Click();
        Browser.Equal($"JavaScript button callback received mouse event args (id=0, buttons=0)", () => app.FindElement(By.Id("message")).Text);

        // Change the callback to one that displays the last ID (0) incremented by 1.
        app.FindElement(By.Id("set-callback-params")).Click();

        // Invoke callback without params (id=1).
        container.FindElement(By.ClassName("js-callback")).Click();
        Browser.Equal($"JavaScript button callback invoked (id=1)", () => app.FindElement(By.Id("message")).Text);

        // Remove all callbacks.
        app.FindElement(By.Id("remove-callback-params")).Click();
        Browser.Equal($"Finished removing callback parameters on component in {containerId}", () => app.FindElement(By.Id("message")).Text);

        // Invoke the callback without params, assert that it no-ops.
        container.FindElement(By.ClassName("js-callback-with-params")).Click();
        Browser.Equal($"Finished removing callback parameters on component in {containerId}", () => app.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void CallsJavaScriptInitializers()
    {
        app.FindElement(By.Id("show-initializer-call-log")).Click();

        var expectedCallLog = "[{\"name\":\"component-with-many-parameters\",\"parameters\":[" +
            "{\"name\":\"StringParam\",\"type\":\"string\"}," +
            "{\"name\":\"IntParam\",\"type\":\"number\"}," +
            "{\"name\":\"LongParam\",\"type\":\"number\"}," +
            "{\"name\":\"FloatParam\",\"type\":\"number\"}," +
            "{\"name\":\"DoubleParam\",\"type\":\"number\"}," +
            "{\"name\":\"DecimalParam\",\"type\":\"number\"}," +
            "{\"name\":\"NullableIntParam\",\"type\":\"number?\"}," +
            "{\"name\":\"NullableLongParam\",\"type\":\"number?\"}," +
            "{\"name\":\"NullableFloatParam\",\"type\":\"number?\"}," +
            "{\"name\":\"NullableDoubleParam\",\"type\":\"number?\"}," +
            "{\"name\":\"NullableDecimalParam\",\"type\":\"number?\"}," +
            "{\"name\":\"BoolParam\",\"type\":\"boolean\"}," +
            "{\"name\":\"NullableBoolParam\",\"type\":\"boolean?\"}," +
            "{\"name\":\"DateTimeParam\",\"type\":\"object\"}," +
            "{\"name\":\"ComplexTypeParam\",\"type\":\"object\"}," +
            "{\"name\":\"JSObjectReferenceParam\",\"type\":\"object\"}]}" +
            "]";

        Browser.Equal(expectedCallLog, () => Browser.FindElement(By.Id("message")).Text);
    }

    void AssertGlobalErrorState(bool hasGlobalError)
    {
        var globalErrorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Browser.Equal(hasGlobalError ? "block" : "none", () => globalErrorUi.GetCssValue("display"));
    }

    ShadowRoot GetShadowRoot(ISearchContext element)
    {
        var result = ((IJavaScriptExecutor)Browser).ExecuteScript("return arguments[0].shadowRoot", element);
        return (ShadowRoot)result;
    }
}
