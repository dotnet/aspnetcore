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

        // Can update parameters
        // Can set complex parameters
        // Can set invalid parameters
        // Can set catch-all parameters
        // Can observe quiescence that completes successfully
        // Can observe quiescence that fails
    }
}
