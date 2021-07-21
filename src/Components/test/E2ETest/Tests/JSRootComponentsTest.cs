// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
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
            Navigate(ServerPathBase, noReload: false);
            app = Browser.MountTestComponent<JavaScriptRootComponents>();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CanAddAndDisposeRootComponents(bool intoBlazorUi)
        {
            var message = app.FindElement(By.Id("message"));

            // We can add root components with initial parameters
            var buttonId = intoBlazorUi ? "add-root-component-inside-blazor" : "add-root-component";
            app.FindElement(By.Id(buttonId)).Click();

            // They render and work
            var containerId = intoBlazorUi ? "container-rendered-by-blazor" : "root-container-1";
            var dynamicRootContainer = Browser.FindElement(By.Id(containerId));
            Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
            dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
            dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
            Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

            // We can dispose the root component
            app.FindElement(By.Id("remove-root-component")).Click();
            Browser.Equal($"Disposed component in {containerId}", () => message.Text);

            // It's gone from the UI
            Browser.Equal(string.Empty, () => dynamicRootContainer.Text);
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
        public void CannotAddMultipleRootComponentsToTheSameElementAtTheSameTime()
        {
            // Try adding a second without removing the first
            app.FindElement(By.Id("add-root-component-inside-blazor")).Click();
            app.FindElement(By.Id("add-root-component-inside-blazor")).Click();

            AssertGlobalErrorState(true);
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
                param => {
                    Assert.Equal("stringVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                    Assert.Equal("String", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                    Assert.Equal("Hello", param.FindElement(By.ClassName("unmatched-value-value")).Text);
                },
                param => {
                    Assert.Equal("wholeNumberVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                    Assert.Equal("Double", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                    Assert.Equal("1", param.FindElement(By.ClassName("unmatched-value-value")).Text);
                },
                param => {
                    Assert.Equal("fractionalNumberVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                    Assert.Equal("Double", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                    Assert.Equal("-123.456", param.FindElement(By.ClassName("unmatched-value-value")).Text);
                },
                param => {
                    Assert.Equal("trueVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                    Assert.Equal("Boolean", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                    Assert.Equal("True", param.FindElement(By.ClassName("unmatched-value-value")).Text);
                },
                param => {
                    Assert.Equal("falseVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                    Assert.Equal("Boolean", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                    Assert.Equal("False", param.FindElement(By.ClassName("unmatched-value-value")).Text);
                },
                param => {
                    Assert.Equal("nullVal", param.FindElement(By.ClassName("unmatched-value-name")).Text);
                    Assert.Equal("null", param.FindElement(By.ClassName("unmatched-value-type")).Text);
                });
        }

        void AssertGlobalErrorState(bool hasGlobalError)
        {
            var globalErrorUi = Browser.Exists(By.Id("blazor-error-ui"));
            Assert.Equal(hasGlobalError ? "block" : "none", globalErrorUi.GetCssValue("display"));
        }
    }
}
