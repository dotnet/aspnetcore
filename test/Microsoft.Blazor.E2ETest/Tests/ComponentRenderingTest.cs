// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.E2ETest.Infrastructure;
using Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Tests
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
            var appElement = MountTestComponent<CounterComponent>();

            Assert.Equal(
                "Current count: 0",
                appElement.FindElement(By.TagName("p")).Text);

            appElement.FindElement(By.TagName("button")).Click();

            Assert.Equal(
                "Current count: 1",
                appElement.FindElement(By.TagName("p")).Text);
        }

        [Fact]
        public void CanTriggerKeyPressEvents()
        {
            var appElement = MountTestComponent<KeyPressEventComponent>();

            Assert.Empty(appElement.FindElements(By.TagName("li")));

            appElement.FindElement(By.TagName("input")).SendKeys("a");
            Assert.Collection(appElement.FindElements(By.TagName("li")),
                li => Assert.Equal("a", li.Text));

            appElement.FindElement(By.TagName("input")).SendKeys("b");
            Assert.Collection(appElement.FindElements(By.TagName("li")),
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
            Assert.Equal("Child component",
                appElement.FindElement(By.CssSelector("fieldset > blazor-component")).Text);
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
