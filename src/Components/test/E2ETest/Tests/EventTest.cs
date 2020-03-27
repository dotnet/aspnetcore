// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class EventTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public EventTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: true);
            Browser.MountTestComponent<EventBubblingComponent>();
        }

        [Fact]
        public void FocusEvents_CanTrigger()
        {
            Browser.MountTestComponent<FocusEventComponent>();

            var input = Browser.FindElement(By.Id("input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            // Focus the target, verify onfocusin is fired
            input.Click();

            Browser.Equal("onfocus,onfocusin,", () => output.Text);

            // Focus something else, verify onfocusout is also fired
            var other = Browser.FindElement(By.Id("other"));
            other.Click();

            Browser.Equal("onfocus,onfocusin,onblur,onfocusout,", () => output.Text);
        }

        [Fact]
        public void MouseOverAndMouseOut_CanTrigger()
        {
            Browser.MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("mouseover_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            var other = Browser.FindElement(By.Id("other"));

            // Mouse over the button and then back off
            var actions = new Actions(Browser)
                .MoveToElement(input)
                .MoveToElement(other);

            actions.Perform();
            Browser.Equal("onmouseover,onmouseout,", () => output.Text);
        }

        [Fact]
        public void MouseMove_CanTrigger()
        {
            Browser.MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("mousemove_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            // Move a little bit
            var actions = new Actions(Browser)
                .MoveToElement(input)
                .MoveToElement(input, 10, 10);

            actions.Perform();
            Browser.Contains("onmousemove,", () => output.Text);
        }

        [Fact]
        public void MouseDownAndMouseUp_CanTrigger()
        {
            Browser.MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("mousedown_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            var other = Browser.FindElement(By.Id("other"));

            // Mousedown
            var actions = new Actions(Browser).ClickAndHold(input);

            actions.Perform();
            Browser.Equal("onmousedown,", () => output.Text);

            actions = new Actions(Browser).Release(input);

            actions.Perform();
            Browser.Equal("onmousedown,onmouseup,", () => output.Text);
        }

        [Fact]
        public void PointerDown_CanTrigger()
        {
            Browser.MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("pointerdown_input"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            var actions = new Actions(Browser).ClickAndHold(input);

            actions.Perform();
            Browser.Equal("onpointerdown", () => output.Text);
        }

        [Fact]
        public void DragDrop_CanTrigger()
        {
            Browser.MountTestComponent<MouseEventComponent>();

            var input = Browser.FindElement(By.Id("drag_input"));
            var target = Browser.FindElement(By.Id("drop"));

            var output = Browser.FindElement(By.Id("output"));
            Assert.Equal(string.Empty, output.Text);

            var actions = new Actions(Browser).DragAndDrop(input, target);

            actions.Perform();
            // drop doesn't seem to trigger in Selenium. But it's sufficient to determine "any" drag event works
            Browser.Equal("ondragstart,", () => output.Text);
        }

        [Fact]
        public void PreventDefault_AppliesToFormOnSubmitHandlers()
        {
            var appElement = Browser.MountTestComponent<EventPreventDefaultComponent>();

            appElement.FindElement(By.Id("form-1-button")).Click();
            Browser.Equal("Event was handled", () => appElement.FindElement(By.Id("event-handled")).Text);
        }

        [Fact]
        public void PreventDefault_DotNotApplyByDefault()
        {
            var appElement = Browser.MountTestComponent<EventPreventDefaultComponent>();
            appElement.FindElement(By.Id("form-2-button")).Click();
            Assert.Contains("about:blank", Browser.Url);
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/1987")]
        public void InputEvent_RespondsOnKeystrokes()
        {
            Browser.MountTestComponent<InputEventComponent>();

            var input = Browser.FindElement(By.TagName("input"));
            var output = Browser.FindElement(By.Id("test-result"));

            Browser.Equal(string.Empty, () => output.Text);

            SendKeysSequentially(input, "abcdefghijklmnopqrstuvwxyz");
            Browser.Equal("abcdefghijklmnopqrstuvwxyz", () => output.Text);

            input.SendKeys(Keys.Backspace);
            Browser.Equal("abcdefghijklmnopqrstuvwxy", () => output.Text);
        }

        [Fact]
        public void InputEvent_RespondsOnKeystrokes_EvenIfUpdatesAreLaggy()
        {
            // This test doesn't mean much on WebAssembly - it just shows that even if the CPU is locked
            // up for a bit it doesn't cause typing to lose keystrokes. But when running server-side, this
            // shows that network latency doesn't cause keystrokes to be lost even if:
            // [1] By the time a keystroke event arrives, the event handler ID has since changed
            // [2] We have the situation described under "the problem" at https://github.com/dotnet/aspnetcore/issues/8204#issuecomment-493986702

            Browser.MountTestComponent<LaggyTypingComponent>();

            var input = Browser.FindElement(By.TagName("input"));
            var output = Browser.FindElement(By.Id("test-result"));

            Browser.Equal(string.Empty, () => output.Text);

            SendKeysSequentially(input, "abcdefg");
            Browser.Equal("abcdefg", () => output.Text);

            SendKeysSequentially(input, "hijklmn");
            Browser.Equal("abcdefghijklmn", () => output.Text);
        }

        [Fact]
        public void NonInteractiveElementWithDisabledAttributeDoesRespondToMouseEvents()
        {
            Browser.MountTestComponent<EventDisablingComponent>();
            var element = Browser.FindElement(By.Id("disabled-div"));
            var eventLog = Browser.FindElement(By.Id("event-log"));

            Browser.Equal(string.Empty, () => eventLog.GetAttribute("value"));
            element.Click();
            Browser.Equal("Got event on div", () => eventLog.GetAttribute("value"));
        }

        [Theory]
        [InlineData("#disabled-button")]
        [InlineData("#disabled-button span")]
        [InlineData("#disabled-textarea")]
        public void InteractiveElementWithDisabledAttributeDoesNotRespondToMouseEvents(string elementSelector)
        {
            Browser.MountTestComponent<EventDisablingComponent>();
            var element = Browser.FindElement(By.CssSelector(elementSelector));
            var eventLog = Browser.FindElement(By.Id("event-log"));

            Browser.Equal(string.Empty, () => eventLog.GetAttribute("value"));
            element.Click();

            // It's no use observing that the log is still empty, since maybe the UI just hasn't updated yet
            // To be sure that the preceding action has no effect, we need to trigger a different action that does have an effect
            Browser.FindElement(By.Id("enabled-button")).Click();
            Browser.Equal("Got event on enabled button", () => eventLog.GetAttribute("value"));
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
