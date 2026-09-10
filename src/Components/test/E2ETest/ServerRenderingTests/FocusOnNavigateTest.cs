// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
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
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetDomAttribute("data-focus-on-navigate") is not null);
    }

    [Fact]
    public void FocusIsPreserved_AfterInitialPageLoad_WhenAnyElementHasFocus()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/static-with-other-focused-element");
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetDomAttribute("data-focus-on-load") is not null);
    }

    [Fact]
    public void FocusIsPreserved_AfterInitialPageLoad_WhenAutofocusedElementIsPresent()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/autofocus");
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetDomAttribute("autofocus") is not null);
    }

    [Fact]
    public void FocusIsPreserved_OnEnhancedNavigation_WhenNoElementMatchesSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate/static");
        Browser.Click(By.LinkText("Home"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().Text == "Home");
    }

    [Fact]
    public void FocusIsMoved_OnEnhancedNavigation_WhenAnyElementMatchesSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate");
        Browser.Click(By.LinkText("Statically rendered"));
        Browser.True(
            () => Browser.SwitchTo().ActiveElement().GetDomAttribute("data-focus-on-navigate") is not null,
            TimeSpan.FromSeconds(5)
        );
    }

    [Fact]
    public void FocusIsPreserved_OnEnhancedFormPost_WhenAnyElementMatchesSelector()
    {
        Navigate($"{ServerPathBase}/focus-on-navigate");
        Browser.Click(By.LinkText("Form submission"));
        string valueToSubmit = "value-to-submit";
        AssertFocusPreserved(valueToSubmit);
        Browser.FindElement(By.Id(valueToSubmit)).ReplaceText("Some value");
        string submitButtonId = "submit-button";
        Browser.Click(By.Id(submitButtonId));
        Browser.Equal("Some value", () => Browser.FindElement(By.Id("submitted-value")).Text);
        AssertFocusPreserved(submitButtonId);
    }

    private void AssertFocusPreserved(string elementId)
    {
        Browser.WaitForElementToBeVisible(By.Id(elementId));
        Browser.True(
            () => Browser.SwitchTo().ActiveElement().GetDomAttribute("id") == elementId,
            TimeSpan.FromSeconds(5),
            $"Expected element with id '{elementId}' to be focused, but found '{Browser.SwitchTo().ActiveElement().GetDomAttribute("id")}' instead."
        );
    }

    [Fact]
    public void FocusIsMoved_AfterStreaming_WhenElementMatchesSelector()
    {
        var tcs = new TaskCompletionSource();
        StreamRenderedComponent.SetStreamingTask(tcs.Task);

        Navigate($"{ServerPathBase}/focus-on-navigate/stream");
        Browser.Equal("Streaming...", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");

        tcs.SetResult();

        Browser.Equal("Complete", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetDomAttribute("data-focus-on-navigate") is not null);
    }

    [Fact]
    public void FocusIsPreserved_AfterStreaming_WhenElementMatchesSelector()
    {
        var tcs = new TaskCompletionSource();
        StreamRenderedComponent.SetStreamingTask(tcs.Task);

        Navigate($"{ServerPathBase}/focus-on-navigate/stream");
        Browser.Equal("Streaming...", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().TagName == "body");

        Browser.Click(By.Id("focusable-input"));
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetDomAttribute("id") == "focusable-input");

        tcs.SetResult();

        Browser.Equal("Complete", () => Browser.FindElement(By.Id("streaming-status")).Text);
        Browser.True(() => Browser.SwitchTo().ActiveElement().GetDomAttribute("id") == "focusable-input");
    }
}
