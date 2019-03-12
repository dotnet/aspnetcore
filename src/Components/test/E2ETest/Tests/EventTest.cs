// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class EventTest : BasicTestAppTestBase
    {
        public EventTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
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

            WaitAssert.Equal("onfocus,onfocusin,", () => output.Text);

            // Focus something else, verify onfocusout is also fired
            var other = Browser.FindElement(By.Id("other"));
            other.Click();

            WaitAssert.Equal("onfocus,onfocusin,onblur,onfocusout,", () => output.Text);
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
            WaitAssert.Equal("onmouseover,onmouseout,", () => output.Text);
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
            WaitAssert.Contains("onmousemove,", () => output.Text);
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
            WaitAssert.Equal("onmousedown,", () => output.Text);

            actions = new Actions(Browser).Release(input);

            actions.Perform();
            WaitAssert.Equal("onmousedown,onmouseup,", () => output.Text);
        }

        [Fact]
        public void PreventDefault_AppliesToFormOnSubmitHandlers()
        {
            var appElement = MountTestComponent<EventPreventDefaultComponent>();

            appElement.FindElement(By.Id("form-1-button")).Click();
            WaitAssert.Equal("Event was handled", () => appElement.FindElement(By.Id("event-handled")).Text);
        }

        [Fact]
        public void PreventDefault_DotNotApplyByDefault()
        {
            var appElement = MountTestComponent<EventPreventDefaultComponent>();
            appElement.FindElement(By.Id("form-2-button")).Click();
            Assert.Contains("about:blank", Browser.Url);
        }

        [Fact]
        public void InputEvent_RespondsOnKeystrokes()
        {
            MountTestComponent<InputEventComponent>();

            var input = Browser.FindElement(By.TagName("input"));
            var output = Browser.FindElement(By.Id("test-result"));

            WaitAssert.Equal(string.Empty, () => output.Text);

            SendKeysSequentially(input, "abcdefghijklmnopqrstuvwxyz");
            WaitAssert.Equal("abcdefghijklmnopqrstuvwxyz", () => output.Text);

            input.SendKeys(Keys.Backspace);
            WaitAssert.Equal("abcdefghijklmnopqrstuvwxy", () => output.Text);
        }

        void SendKeysSequentially(IWebElement target, string text)
        {
            // Calling it for each character works around some chars being skipped
            // https://stackoverflow.com/a/40986041
            foreach (var c in text)
            {
                target.SendKeys(c.ToString());
            }
        }
    }
}
