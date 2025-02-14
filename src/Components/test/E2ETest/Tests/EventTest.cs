// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

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
        Navigate(ServerPathBase);
    }

    [Fact]
    public void FocusEvents_CanTrigger()
    {
        Browser.MountTestComponent<FocusEventComponent>();

        var input = Browser.Exists(By.Id("input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Focus the target, verify onfocusin is fired
        input.Click();

        Browser.Equal("focus,focusin,", () => output.Text);

        // Focus something else, verify onfocusout is also fired
        var other = Browser.Exists(By.Id("other"));
        other.Click();

        Browser.Equal("focus,focusin,blur,focusout,", () => output.Text);
    }

    [Fact]
    public void FocusEvents_CanReceiveBlurCausedByElementRemoval()
    {
        // Represents https://github.com/dotnet/aspnetcore/issues/26838

        Browser.MountTestComponent<FocusEventComponent>();

        Browser.FindElement(By.Id("button-that-disappears")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("button-received-focus-out")).Text);
    }

    [Fact]
    public void MouseOverAndMouseOut_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("mouseover_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        var other = Browser.Exists(By.Id("mouseover_label"));

        // Mouse over the button and then back off
        var actions = new Actions(Browser)
            .MoveToElement(input)
            .MoveToElement(other);

        actions.Perform();
        Browser.Equal("mouseover,mouseout,", () => output.Text);
    }

    [Fact]
    public void MouseEnterAndMouseLeave_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("mouseenter_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Mouse enter the button and then mouse leave
        Browser.ExecuteJavaScript($@"
            var mouseEnterElement = document.getElementById('mouseenter_input');
            var mouseEnterEvent = new MouseEvent('mouseenter');
            var mouseLeaveEvent = new MouseEvent('mouseleave');
            mouseEnterElement.dispatchEvent(mouseEnterEvent);
            mouseEnterElement.dispatchEvent(mouseLeaveEvent);");

        Browser.Equal("mouseenter,mouseleave,", () => output.Text);
    }

    [Fact]
    public void PointerEnterAndPointerLeave_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("pointerenter_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Pointer enter the button and then pointer leave
        Browser.ExecuteJavaScript($@"
            var pointerEnterElement = document.getElementById('pointerenter_input');
            var pointerEnterEvent = new PointerEvent('pointerenter');
            var pointerLeaveEvent = new PointerEvent('pointerleave');
            pointerEnterElement.dispatchEvent(pointerEnterEvent);
            pointerEnterElement.dispatchEvent(pointerLeaveEvent);");

        Browser.Equal("pointerenter,pointerleave,", () => output.Text);
    }

    [Fact]
    public void MouseMove_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("mousemove_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Move a little bit
        var actions = new Actions(Browser)
            .MoveToElement(input)
            .MoveToElement(input, 10, 10);

        actions.Perform();
        Browser.Contains("mousemove,", () => output.Text);
    }

    [Fact]
    public void MouseDownAndMouseUp_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("mousedown_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Mousedown
        var actions = new Actions(Browser).ClickAndHold(input);

        actions.Perform();
        Browser.Equal("mousedown,", () => output.Text);

        actions = new Actions(Browser).Release(input);

        actions.Perform();
        Browser.Equal("mousedown,mouseup,", () => output.Text);
    }

    [Fact]
    public void Toggle_CanTrigger()
    {
        Browser.MountTestComponent<ToggleEventComponent>();

        var detailsToggle = Browser.Exists(By.Id("details-toggle"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Click
        var actions = new Actions(Browser).Click(detailsToggle);

        actions.Perform();
        Browser.Equal("ontoggle,", () => output.Text);
    }

    [Fact]
    public void Close_CanTrigger()
    {
        Browser.MountTestComponent<DialogEventsComponent>();

        Browser.Exists(By.Id("show-dialog")).Click();

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Click
        Browser.Exists(By.Id("dialog-close")).Click();
        Browser.Equal("onclose,", () => output.Text);
    }

    [Fact]
    public void Cancel_CanTrigger()
    {
        Browser.MountTestComponent<DialogEventsComponent>();

        Browser.Exists(By.Id("show-dialog")).Click();

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // Press escape to cancel. This fires both close and cancel, but MDN doesn't document in which order
        Browser.FindElement(By.Id("my-dialog")).SendKeys(Keys.Escape);
        Browser.Contains("onclose,", () => output.Text);
        Browser.Contains("oncancel,", () => output.Text);
    }

    [Fact]
    public void PointerDown_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("pointerdown_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        var actions = new Actions(Browser).ClickAndHold(input);

        actions.Perform();
        Browser.Equal("pointerdown,", () => output.Text);
    }

    [Fact]
    public void DragDrop_CanTrigger()
    {
        Browser.MountTestComponent<MouseEventComponent>();

        var input = Browser.Exists(By.Id("drag_input"));
        var target = Browser.Exists(By.Id("drop"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        var actions = new Actions(Browser).DragAndDrop(input, target);

        actions.Perform();
        // drop doesn't reliably trigger in Selenium. But it's sufficient to determine "any" drag event works
        Browser.True(() => output.Text.StartsWith("dragstart,", StringComparison.Ordinal));
    }

    [Fact]
    public void TouchEvent_CanTrigger()
    {
        Browser.MountTestComponent<TouchEventComponent>();

        var input = Browser.Exists(By.Id("touch_input"));

        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        var touchPointer = new PointerInputDevice(PointerKind.Touch);
        var singleTap = new ActionBuilder()
            .AddAction(touchPointer.CreatePointerMove(input, 0, 0, TimeSpan.Zero))
            .AddAction(touchPointer.CreatePointerDown(MouseButton.Touch))
            .AddAction(touchPointer.CreatePointerUp(MouseButton.Touch));
        ((IActionExecutor)Browser).PerformActions(singleTap.ToActionSequenceList());

        Browser.Equal("touchstart,touchend,", () => output.Text);
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
    public void InputEvent_RespondsOnKeystrokes()
    {
        Browser.MountTestComponent<InputEventComponent>();

        var input = Browser.Exists(By.TagName("input"));
        var output = Browser.Exists(By.Id("test-result"));

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

        var input = Browser.Exists(By.TagName("input"));
        var output = Browser.Exists(By.Id("test-result"));

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
        var element = Browser.Exists(By.Id("disabled-div"));
        var eventLog = Browser.Exists(By.Id("event-log"));

        Browser.Equal(string.Empty, () => eventLog.GetDomProperty("value"));
        element.Click();
        Browser.Equal("Got event on div", () => eventLog.GetDomProperty("value"));
    }

    [Theory]
    [InlineData("#disabled-button")]
    [InlineData("#disabled-button span")]
    [InlineData("#disabled-textarea")]
    public void InteractiveElementWithDisabledAttributeDoesNotRespondToMouseEvents(string elementSelector)
    {
        Browser.MountTestComponent<EventDisablingComponent>();
        var element = Browser.Exists(By.CssSelector(elementSelector));
        var eventLog = Browser.Exists(By.Id("event-log"));

        Browser.Equal(string.Empty, () => eventLog.GetDomProperty("value"));
        element.Click();

        // It's no use observing that the log is still empty, since maybe the UI just hasn't updated yet
        // To be sure that the preceding action has no effect, we need to trigger a different action that does have an effect
        Browser.Exists(By.Id("enabled-button")).Click();
        Browser.Equal("Got event on enabled button", () => eventLog.GetDomProperty("value"));
    }

    [Fact]
    public virtual void EventDuringBatchRendering_CanTriggerDOMEvents()
    {
        Browser.MountTestComponent<EventDuringBatchRendering>();

        var input = Browser.Exists(By.CssSelector("#reversible-list input"));
        var eventLog = Browser.Exists(By.Id("event-log"));

        SendKeysSequentially(input, "abc");
        Browser.Equal("abc", () => input.GetDomProperty("value"));
        Browser.Equal(
            "Change event on item First with value a\n" +
            "Change event on item First with value ab\n" +
            "Change event on item First with value abc",
            () => eventLog.Text.Trim().Replace("\r\n", "\n"));
    }

    [Fact]
    public void EventDuringBatchRendering_CannotTriggerJSInterop()
    {
        Browser.MountTestComponent<EventDuringBatchRendering>();
        var errorLog = Browser.Exists(By.Id("web-component-error-log"));

        Browser.Exists(By.Id("add-web-component")).Click();
        var expectedMessage = _serverFixture.ExecutionMode == ExecutionMode.Client
            ? "Assertion failed - heap is currently locked"
            : "There was an exception invoking 'SomeMethodThatDoesntNeedToExistForThisTest' on assembly 'SomeAssembly'";

        Browser.Contains(expectedMessage, () => errorLog.Text);
    }

    [Fact]
    public void RenderAttributesBeforeConnectedCallBack()
    {
        Browser.MountTestComponent<RenderAttributesBeforeConnectedCallback>();
        var element = Browser.Exists(By.TagName("custom-web-component-data-from-attribute"));

        var expectedContent = "success";

        Browser.Contains(expectedContent, () => element.Text);
    }

    [Fact]
    public void PolymorphicEventHandlersReceiveCorrectArgsSubclass()
    {
        // This is to show that the type of event argument received corresponds to the declared event
        // name, and not to the argument type on the event handler delegate. Note that this is only
        // supported (for back-compat) for the built-in standard web event types. For custom events,
        // the eventargs deserialization type is determined purely by the delegate's parameters list.
        Browser.MountTestComponent<MouseEventComponent>();

        var elem = Browser.Exists(By.Id("polymorphic_event_elem"));

        // Output is initially empty
        var output = Browser.Exists(By.Id("output"));
        Assert.Equal(string.Empty, output.Text);

        // We can trigger a pointer event and receive a PointerEventArgs
        new Actions(Browser).Click(elem).Perform();
        Browser.Equal("Microsoft.AspNetCore.Components.Web.PointerEventArgs:mouse", () => output.Text);

        // We can trigger a drag event and receive a DragEventArgs *on the same handler delegate*
        Browser.FindElement(By.Id("clear_event_log")).Click();
        new Actions(Browser).DragAndDrop(elem, Browser.FindElement(By.Id("other"))).Perform();
        Browser.Equal("Microsoft.AspNetCore.Components.Web.DragEventArgs:1", () => output.Text);
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
