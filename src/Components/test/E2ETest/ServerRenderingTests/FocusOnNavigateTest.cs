// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using TestServer;
using Xunit.Abstractions;
using StreamRenderedComponent = Components.TestServer.RazorComponents.Pages.FocusOnNavigate.StreamRendered;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class FocusOnNavigateTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public FocusOnNavigateTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void FocusIsMoved_AfterInitialPageLoad_WhenNoElementHasFocus()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/static");
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("data-focus-on-navigate") is not null);
    }

    [Fact]
    public void FocusIsPreserved_AfterInitialPageLoad_WhenAnyElementHasFocus()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/static-with-other-focused-element");
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("data-focus-on-load") is not null);
    }

    [Fact]
    public void FocusIsPreserved_OnEnhancedNavigation_WhenNoElementMatchesSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/static");
        WaitUntilDocumentReady();
        Browser.Click(By.LinkText("Home"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().Text == "Home");
    }

    [Fact]
    public void FocusIsMoved_OnEnhancedNavigation_WhenAnyElementMatchesSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate");
        WaitUntilDocumentReady();
        Browser.Click(By.LinkText("Statically rendered"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("data-focus-on-navigate") is not null);
    }

    [Fact]
    public void FocusIsPreserved_OnEnhancedFormPost_WhenAnyElementMatchesSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/form-submission");
        WaitUntilDocumentReady();
        Browser.Click(By.LinkText("Form submission"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "value-to-submit");
        Browser.FindElement(By.Id("value-to-submit")).ReplaceText("Some value");
        Browser.Click(By.Id("submit-button"));
        Browser.Equal("Some value", () => Browser.FindElement(By.Id("submitted-value")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "submit-button");
    }

    [Fact]
    public void FocusIsMoved_OnStreamingUpdate_WhenElementMatchingSelectorGetsAddedToDocument()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/stream");
        Browser.Equal("Streaming...", () => Browser.FindElement(By.Id("streaming-status")).Text);

        // Add an element that does NOT match the focus selector.
        StreamRenderedComponent.AddElement(new(WantsFocus: false));
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");
        Browser.Exists(By.Id("input-element-0"));

        // Add an element that does match the focus selector. It should receive focus.
        StreamRenderedComponent.AddElement(new(WantsFocus: true));
        Browser.Exists(By.Id("input-element-1"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-1");

        StreamRenderedComponent.EndResponse();
        Browser.Equal("Complete", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-1");
    }

    [Fact]
    public void FocusIsPreserved_OnStreamingUpdate_WhenUserFocusesElementNotMatchingSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/stream");
        Browser.Equal("Streaming...", () => Browser.FindElement(By.Id("streaming-status")).Text);

        // Add an element that does not get autofocused, but then manually focus it
        StreamRenderedComponent.AddElement(new(WantsFocus: false));
        Browser.Click(By.Id("input-element-0"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-0");

        // Add an element that wants to get autofocused. However, it should not receive focus
        // because the user has already focused the previous element
        StreamRenderedComponent.AddElement(new(WantsFocus: true));
        Browser.Exists(By.Id("input-element-1"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-0");

        StreamRenderedComponent.EndResponse();
        Browser.Equal("Complete", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-0");
    }

    [Fact]
    public void FocusIsMoved_OnStreamingUpdate_WhenElementMatchingSelectorGetsRemovedFromDocument_ThenAddedBack()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/stream");
        Browser.Equal("Streaming...", () => Browser.FindElement(By.Id("streaming-status")).Text);

        // Add an element that does NOT match the focus selector.
        StreamRenderedComponent.AddElement(new(WantsFocus: false));
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");
        Browser.Exists(By.Id("input-element-0"));

        // Add an element that does match the focus selector. It should receive focus.
        StreamRenderedComponent.AddElement(new(WantsFocus: true));
        Browser.Exists(By.Id("input-element-1"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-1");

        // Remove the element that received focus.
        StreamRenderedComponent.RemoveElement(index: 1);
        Browser.DoesNotExist(By.Id("input-element-1"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");

        // Add an element back that matches the focus selector. It should receive focus once again.
        StreamRenderedComponent.AddElement(new(WantsFocus: true));
        Browser.Exists(By.Id("input-element-1"));

        StreamRenderedComponent.EndResponse();
        Browser.Equal("Complete", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-1");
    }

    [Fact]
    public void FocusIsPreserved_OnStreamingUpdate_WhenElementMatchingSelectorGetsRemovedFromDocument_ThenAddedBack_AfterUserFocusesDifferentElement()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/stream");
        Browser.Equal("Streaming...", () => Browser.FindElement(By.Id("streaming-status")).Text);

        // Add an element that does NOT match the focus selector.
        StreamRenderedComponent.AddElement(new(WantsFocus: false));
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");
        Browser.Exists(By.Id("input-element-0"));

        // Add an element that does match the focus selector. It should receive focus.
        StreamRenderedComponent.AddElement(new(WantsFocus: true));
        Browser.Exists(By.Id("input-element-1"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-1");

        // Remove the element that received focus.
        StreamRenderedComponent.RemoveElement(index: 1);
        Browser.DoesNotExist(By.Id("input-element-1"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");

        // Focus a different element by clicking it.
        Browser.Click(By.Id("input-element-0"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-0");

        // Add an element back that matches the focus selector. It should not receive focus because
        // the user has explicitly focused the previous element.
        StreamRenderedComponent.AddElement(new(WantsFocus: true));
        Browser.Exists(By.Id("input-element-1"));

        StreamRenderedComponent.EndResponse();
        Browser.Equal("Complete", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("id") == "input-element-0");
    }

    [Fact]
    public void FocusIsRestored_AfterInteractivityStarts_WhenElementMatchingSelectorWasRemoved()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate");
        WaitUntilDocumentReady();
        Browser.Click(By.LinkText("Interactively rendered"));
        Browser.Equal("interactive", () => Browser.FindElement(By.Id("focus-on-nav-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("data-focus-on-navigate") is not null);
    }

    [Fact]
    public void FocusIsNotRestored_AfterInteractivityStarts_WhenFocusedElementWasSelectedByUser()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/interactive-with-other-focused-element");
        Browser.Equal("interactive", () => Browser.FindElement(By.Id("focus-on-nav-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetAttribute("data-focus-on-load") is not null);
    }

    private void WaitUntilDocumentReady()
    {
        Browser.True(() => Browser.ExecuteJavaScript<bool>("return document.readyState !== 'loading';"));
    }
}
