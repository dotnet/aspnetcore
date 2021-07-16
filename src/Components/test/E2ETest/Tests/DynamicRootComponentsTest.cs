// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    // Note that this tests dynamic *root* components, not the <DynamicComponent> component.
    // See DynamicComponentRenderingTest.cs for tests about <DynamicComponent>.

    public class DynamicRootComponentsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        private IWebElement app;

        public DynamicRootComponentsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: false);
            app = Browser.MountTestComponent<DynamicRootComponents>();
        }

        [Fact]
        public void CanAddAndDisposeRootComponents()
        {
            var message = app.FindElement(By.Id("message"));



            // We can add root components
            app.FindElement(By.Id("add-root-component")).Click();

            // They don't render until they receive parameters
            var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
            Browser.Empty(() => dynamicRootContainer.FindElements(By.CssSelector("*")));

            // They do render when they do receive parameters
            app.FindElement(By.Id("set-increment-amount")).Click();
            Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
            dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
            dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
            Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

            // We can dispose the root component
            app.FindElement(By.Id("remove-root-component")).Click();
            Browser.Equal("Disposed component in root-container-1", () => message.Text);

            // Although it's disposed from the Blazor component hierarchy, the inert DOM elements remain
            // in the document since it's up to the JS developer to remove them if (and only if) they want
            Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

            // Since it's not part of the Blazor component hierarchy, it no longer reacts to events
            dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
            Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
        }

        [Fact]
        public void CanUpdateParameters()
        {
            // Create the initial component
            app.FindElement(By.Id("add-root-component")).Click();
            var setIncrementAmount = app.FindElement(By.Id("set-increment-amount"));
            setIncrementAmount.Click();
            var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
            var incrementButton = dynamicRootContainer.FindElement(By.ClassName("increment"));
            var clickCount = dynamicRootContainer.FindElement(By.ClassName("click-count"));
            incrementButton.Click();
            Browser.Equal("1", () => clickCount.Text);

            // Supply updated parameters
            var incrementAmount = app.FindElement(By.Id("increment-amount"));
            incrementAmount.Clear();
            incrementAmount.SendKeys("4");
            setIncrementAmount.Click();
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
            Browser.Contains("Error: System.Text.Json.JsonException", () => app.FindElement(By.Id("message")).Text);
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

        [Fact]
        public void CanObserveQuiescenceFromSetParametersCall()
        {
            app.FindElement(By.Id("add-root-component")).Click();
            app.FindElement(By.Id("onparametersset-pause")).Click();
            app.FindElement(By.Id("set-increment-amount")).Click();

            // Although it's done its initial synchronous render, the OnParametersSetAsync code returned
            // an incomplete task so we're still waiting
            var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
            var message = app.FindElement(By.Id("message"));
            Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
            Browser.Equal("Calling setParameters on component in root-container-1...", () => message.Text);

            // When the task completes, the promise resolves
            Browser.FindElement(By.Id("onparametersset-success")).Click();
            Browser.Equal("Updated parameters on component in root-container-1", () => message.Text);
        }

        [Fact]
        public void CanObserveQuiescenceFromSetParametersCallWithException()
        {
            app.FindElement(By.Id("add-root-component")).Click();
            app.FindElement(By.Id("onparametersset-pause")).Click();
            app.FindElement(By.Id("set-increment-amount")).Click();

            // Although it's done its initial synchronous render, the OnParametersSetAsync code returned
            // an incomplete task so we're still waiting
            var dynamicRootContainer = Browser.FindElement(By.Id("root-container-1"));
            var message = app.FindElement(By.Id("message"));
            Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
            Browser.Equal("Calling setParameters on component in root-container-1...", () => message.Text);

            // If the task completes with an exception, the promise still succeeds (because component
            // rendering errors are always reported out of band, since rendering exceptions can occur
            // at any time). The error is reported via the blazor-error-ui elemeent.
            Browser.FindElement(By.Id("onparametersset-failure")).Click();
            Browser.Equal("Updated parameters on component in root-container-1", () => message.Text);
            Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));
        }
    }
}
