// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using BasicTestApp;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class ComponentRenderingTest
        : ServerTestBase<DevHostServerFixture<BasicTestApp.Program>>
    {
        public ComponentRenderingTest(BrowserFixture browserFixture, DevHostServerFixture<Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
            Navigate("/", noReload: true);
        }

        [Fact]
        public void BasicTestAppCanBeServed()
        {
            Assert.Equal("Basic test app", Browser.Title);
        }

        [Fact]
        public void CanRenderTextOnlyComponent()
        {
            var appElement = MountTestComponent<TextOnlyComponent>();
            Assert.Equal("Hello from TextOnlyComponent", appElement.Text);
        }

        [Fact]
        public void CanRenderComponentWithAttributes()
        {
            var appElement = MountTestComponent<RedTextComponent>();
            var styledElement = appElement.FindElement(By.TagName("h1"));
            Assert.Equal("Hello, world!", styledElement.Text);
            Assert.Equal("color: red;", styledElement.GetAttribute("style"));
            Assert.Equal("somevalue", styledElement.GetAttribute("customattribute"));
        }

        [Fact]
        public void CanTriggerEvents()
        {
            // Initial count is zero
            var appElement = MountTestComponent<CounterComponent>();
            var countDisplayElement = appElement.FindElement(By.TagName("p"));
            Assert.Equal("Current count: 0", countDisplayElement.Text);

            // Clicking button increments count
            appElement.FindElement(By.TagName("button")).Click();
            Assert.Equal("Current count: 1", countDisplayElement.Text);
        }

        [Fact]
        public void CanTriggerKeyPressEvents()
        {
            // List is initially empty
            var appElement = MountTestComponent<KeyPressEventComponent>();
            var inputElement = appElement.FindElement(By.TagName("input"));
            Func<IEnumerable<IWebElement>> liElements =
                () => appElement.FindElements(By.TagName("li"));
            Assert.Empty(liElements());

            // Typing adds element
            inputElement.SendKeys("a");
            Assert.Collection(liElements(),
                li => Assert.Equal("a", li.Text));

            // Typing again adds another element
            inputElement.SendKeys("b");
            Assert.Collection(liElements(),
                li => Assert.Equal("a", li.Text),
                li => Assert.Equal("b", li.Text));
        }

        [Fact]
        public void CanRenderChildComponents()
        {
            var appElement = MountTestComponent<ParentChildComponent>();
            Assert.Equal("Parent component",
                appElement.FindElement(By.CssSelector("fieldset > legend")).Text);

            // TODO: Once we remove the wrapper elements from around child components,
            // assert that the child component text node is directly inside the <fieldset>
            var childComponentWrapper = appElement.FindElement(By.CssSelector("fieldset > blazor-component"));
            Assert.Single(childComponentWrapper.FindElements(By.CssSelector("*")));

            var styledElement = childComponentWrapper.FindElement(By.TagName("h1"));
            Assert.Equal("Hello, world!", styledElement.Text);
            Assert.Equal("color: red;", styledElement.GetAttribute("style"));
            Assert.Equal("somevalue", styledElement.GetAttribute("customattribute"));
        }

        [Fact]
        public void CanTriggerEventsOnChildComponents()
        {
            // Counter is displayed as child component. Initial count is zero.
            var childComponentWrapper = MountTestComponent<CounterComponentWrapper>()
                .FindElements(By.CssSelector("blazor-component")).Single();
            var counterDisplay = childComponentWrapper.FindElement(By.TagName("p"));
            Assert.Equal("Current count: 0", counterDisplay.Text);

            // Clicking increments count in child component
            childComponentWrapper.FindElement(By.TagName("button")).Click();
            Assert.Equal("Current count: 1", counterDisplay.Text);
        }

        [Fact]
        public void ChildComponentsRerenderWhenPropertiesChanged()
        {
            // Count value is displayed in child component with initial value zero
            var appElement = MountTestComponent<CounterComponentUsingChild>();
            var wholeCounterElement = appElement.FindElement(By.TagName("p"));
            var messageElementInChild = wholeCounterElement.FindElement(By.ClassName("message"));
            Assert.Equal("Current count: 0", wholeCounterElement.Text);
            Assert.Equal("0", messageElementInChild.Text);

            // Clicking increments count in child element
            appElement.FindElement(By.TagName("button")).Click();
            Assert.Equal("1", messageElementInChild.Text);
        }

        private IWebElement MountTestComponent<TComponent>() where TComponent: IComponent
        {
            var componentTypeName = typeof(TComponent).FullName;
            WaitUntilDotNetRunningInBrowser();
            ((IJavaScriptExecutor)Browser).ExecuteScript(
                $"mountTestComponent('{componentTypeName}')");
            return Browser.FindElement(By.TagName("app"));
        }

        private void WaitUntilDotNetRunningInBrowser()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(driver =>
            {
                return ((IJavaScriptExecutor)driver)
                    .ExecuteScript("return window.isTestReady;");
            });
        }
    }
}
