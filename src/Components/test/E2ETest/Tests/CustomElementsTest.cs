// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class CustomElementsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    protected IWebElement app;

    public CustomElementsTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        app = Browser.MountTestComponent<CustomElementsComponent>();
    }

    [Fact]
    public void CanAddAndRemoveCustomElements()
    {
        // Custom elements can be added.
        app.FindElement(By.Id("add-custom-element")).Click();
        Browser.Exists(By.Id("custom-element-0"));

        app.FindElement(By.Id("add-custom-element")).Click();
        Browser.Exists(By.Id("custom-element-1"));

        // Custom elements are correctly removed.
        app.FindElement(By.Id("remove-custom-element")).Click();
        Browser.DoesNotExist(By.Id("custom-element-1"));

        app.FindElement(By.Id("remove-custom-element")).Click();
        Browser.DoesNotExist(By.Id("custom-element-0"));
    }

    [Fact]
    public void CanUpdateSimpleParameters()
    {
        app.FindElement(By.Id("add-custom-element")).Click();
        Browser.Exists(By.Id("custom-element-0"));

        // Initial parameter values are correct.
        ValidateSimpleParameterValues(id: 0, clickCount: 0);

        app.FindElement(By.Id("increment-0")).Click();

        // Parameter values have been updated.
        ValidateSimpleParameterValues(id: 0, clickCount: 1);

        void ValidateSimpleParameterValues(int id, int clickCount)
        {
            // Nullable parameters will be "null" every other click.
            var doNullableParamsHaveValues = clickCount % 2 == 0;

            var customElement = app.FindElement(By.Id($"custom-element-{id}"));

            var expectedStringValue = $"Custom element {id} (Clicked {clickCount} times)";
            Browser.Equal(expectedStringValue, () => customElement.FindElement(By.ClassName("string-param")).Text);

            var expectedBoolValue = clickCount % 2 == 0 ? bool.TrueString : bool.FalseString;
            Browser.Equal(expectedBoolValue, () => customElement.FindElement(By.ClassName("bool-param")).Text);

            var expectedNullableBoolValue = doNullableParamsHaveValues ? expectedBoolValue : "null";
            Browser.Equal(expectedNullableBoolValue, () => customElement.FindElement(By.ClassName("nullable-bool-param")).Text);

            var expectedIntegerValue = clickCount.ToString(CultureInfo.InvariantCulture);
            Browser.Equal(expectedIntegerValue, () => customElement.FindElement(By.ClassName("int-param")).Text);
            Browser.Equal(expectedIntegerValue, () => customElement.FindElement(By.ClassName("long-param")).Text);

            var expectedNullableIntegerValue = doNullableParamsHaveValues ? expectedIntegerValue : "null";
            Browser.Equal(expectedNullableIntegerValue, () => customElement.FindElement(By.ClassName("nullable-int-param")).Text);
            Browser.Equal(expectedNullableIntegerValue, () => customElement.FindElement(By.ClassName("nullable-long-param")).Text);

            var expectedFloatValue = expectedIntegerValue + ".5";
            Browser.Equal(expectedFloatValue, () => customElement.FindElement(By.ClassName("float-param")).Text);
            Browser.Equal(expectedFloatValue, () => customElement.FindElement(By.ClassName("double-param")).Text);
            Browser.Equal(expectedFloatValue, () => customElement.FindElement(By.ClassName("decimal-param")).Text);

            var expectedNullableFloatValue = doNullableParamsHaveValues ? expectedFloatValue : "null";
            Browser.Equal(expectedNullableFloatValue, () => customElement.FindElement(By.ClassName("nullable-float-param")).Text);
            Browser.Equal(expectedNullableFloatValue, () => customElement.FindElement(By.ClassName("nullable-double-param")).Text);
            Browser.Equal(expectedNullableFloatValue, () => customElement.FindElement(By.ClassName("nullable-decimal-param")).Text);
        }
    }

    [Fact]
    public void CanUpdateComplexParameters()
    {
        app.FindElement(By.Id("add-custom-element")).Click();
        Browser.Exists(By.Id("custom-element-0"));

        var incrementButton = app.FindElement(By.Id("increment-0"));
        incrementButton.Click();
        incrementButton.Click();

        app.FindElement(By.Id("update-complex-parameters-0")).Click();

        // The complex object parameter was updated.
        var expectedComplexObjectValue = @"{ Property = ""Clicked 2 times"" }";
        Browser.Equal(expectedComplexObjectValue, () => app.FindElement(By.Id("custom-element-0")).FindElement(By.ClassName("complex-type-param")).Text);

        app.FindElement(By.Id("custom-element-0")).FindElement(By.ClassName("invoke-callback")).Click();

        // The callback parameter was invoked.
        var expectedMessage = "Callback with count = 2";
        Browser.Equal(expectedMessage, () => app.FindElement(By.Id("message")).Text);
    }
}
