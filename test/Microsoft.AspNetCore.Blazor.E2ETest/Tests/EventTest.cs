// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class EventTest : BasicTestAppTestBase
    {
        public EventTest(
            BrowserFixture browserFixture, 
            DevHostServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            Navigate(ServerPathBase, noReload: true);
            MountTestComponent<EventBubblingComponent>();
        }

        [Fact]
        public void FocusEvents_CanTrigger()
        {
            MountTestComponent<FocusEventComponent>();

            var input = Browser.FindElement(By.Id("input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            // Focus the target, verify onfocusin is fired
            input.Click();

            Assert.Equal("onfocus,onfocusin,", output.Text);

            // Focus something else, verify onfocusout is also fired
            var other = Browser.FindElement(By.Id("other"));
            other.Click();

            Assert.Equal("onfocus,onfocusin,onblur,onfocusout,", output.Text);
        }

        [Fact]
        public void MouseOverAndMouseOut_CanTrigger()
        {
            MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("mouseover_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            var other = Browser.FindElement(By.Id("other"));

            // Mouse over the button and then back off
            var actions = new Actions(Browser)
                .MoveToElement(input)
                .MoveToElement(other);

            actions.Perform();
            Assert.Equal("onmouseover,onmouseout,", output.Text);
        }

        [Fact]
        public void MouseMove_CanTrigger()
        {
            MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("mousemove_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            // Move a little bit
            var actions = new Actions(Browser)
                .MoveToElement(input)
                .MoveToElement(input, 10, 10);

            actions.Perform();
            Assert.Contains("onmousemove,", output.Text);
        }

        [Fact]
        public void MouseDownAndMouseUp_CanTrigger()
        {
            MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("mousedown_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            var other = Browser.FindElement(By.Id("other"));

            // Mousedown
            var actions = new Actions(Browser).ClickAndHold(input);

            actions.Perform();
            Assert.Equal("onmousedown,", output.Text);

            actions = new Actions(Browser).Release(input);

            actions.Perform();
            Assert.Equal("onmousedown,onmouseup,", output.Text);
        }

        private string[] GetLogLines()
            => Browser.FindElement(By.TagName("textarea"))
            .GetAttribute("value")
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        private void TriggerCustomBubblingEvent(string elementId, string eventName)
        {
            var jsExecutor = (IJavaScriptExecutor)Browser;
            jsExecutor.ExecuteScript(
                $"document.getElementById('{elementId}').dispatchEvent(" +
                $"    new Event('{eventName}', {{ bubbles: true }})" +
                $")");
            MountTestComponent<FocusEventComponent>();
        }
    }
}
