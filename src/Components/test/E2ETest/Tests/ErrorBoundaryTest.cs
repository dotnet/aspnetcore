// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.ErrorBoundaryTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class ErrorBoundaryTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public ErrorBoundaryTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        // Many of these tests trigger fatal exceptions, so we always have to reload
        Navigate(ServerPathBase);
        Browser.MountTestComponent<ErrorBoundaryContainer>();
    }

    [Theory]
    [InlineData("event-sync")]
    [InlineData("event-async")]
    [InlineData("parametersset-sync")]
    [InlineData("parametersset-async")]
    [InlineData("parametersset-cascade-sync")]
    [InlineData("parametersset-cascade-async")]
    [InlineData("afterrender-sync")]
    [InlineData("afterrender-async")]
    [InlineData("while-rendering")]
    [InlineData("dispatch-sync-exception")]
    [InlineData("dispatch-async-exception")]
    public void CanHandleExceptions(string triggerId)
    {
        var container = Browser.Exists(By.Id("error-boundary-container"));
        container.FindElement(By.Id(triggerId)).Click();

        // The whole UI within the container is replaced by the default error UI
        Browser.Collection(() => container.FindElements(By.CssSelector("*")),
            elem =>
            {
                Assert.Equal("blazor-error-boundary", elem.GetAttribute("class"));
                Assert.Empty(elem.FindElements(By.CssSelector("*")));
            });

        AssertGlobalErrorState(false);
    }

    [Fact]
    public void CanCreateCustomErrorBoundary()
    {
        var container = Browser.Exists(By.Id("custom-error-boundary-test"));
        Func<IWebElement> incrementButtonAccessor = () => container.FindElement(By.ClassName("increment-count"));
        Func<string> currentCountAccessor = () => container.FindElement(By.ClassName("current-count")).Text;

        incrementButtonAccessor().Click();
        incrementButtonAccessor().Click();
        Browser.Equal("2", currentCountAccessor);

        // If it throws, we see the custom error boundary
        container.FindElement(By.ClassName("throw-counter-exception")).Click();
        Browser.Collection(() => container.FindElements(By.ClassName("received-exception")),
            elem => Assert.Equal($"Exception from {nameof(ErrorCausingCounter)}", elem.Text));
        AssertGlobalErrorState(false);

        // On recovery, the count is reset, because it's a new instance
        container.FindElement(By.ClassName("recover")).Click();
        Browser.Equal("0", currentCountAccessor);
        incrementButtonAccessor().Click();
        Browser.Equal("1", currentCountAccessor);
        Browser.Empty(() => container.FindElements(By.ClassName("received-exception")));
    }

    [Fact]
    public void HandleCustomErrorBoundaryThatIgnoresErrors()
    {
        var container = Browser.Exists(By.Id("error-ignorer-test"));
        Func<IWebElement> incrementButtonAccessor = () => container.FindElement(By.ClassName("increment-count"));
        Func<string> currentCountAccessor = () => container.FindElement(By.ClassName("current-count")).Text;

        incrementButtonAccessor().Click();
        incrementButtonAccessor().Click();
        Browser.Equal("2", currentCountAccessor);

        // If it throws, the child content gets forcibly rebuilt even if the error boundary tries to retain it
        container.FindElement(By.ClassName("throw-counter-exception")).Click();
        Browser.Equal("0", currentCountAccessor);
        incrementButtonAccessor().Click();
        Browser.Equal("1", currentCountAccessor);
        AssertGlobalErrorState(false);
    }

    [Fact]
    public void CanHandleErrorsInlineInErrorBoundaryContent()
    {
        var container = Browser.Exists(By.Id("inline-error-test"));
        Browser.Equal("Hello!", () => container.FindElement(By.ClassName("normal-content")).Text);
        Assert.Empty(container.FindElements(By.ClassName("error-message")));

        // If ChildContent throws during rendering, the error boundary handles it
        container.FindElement(By.ClassName("throw-in-childcontent")).Click();
        Browser.Contains("There was an error: System.InvalidTimeZoneException: Inline exception", () => container.FindElement(By.ClassName("error-message")).Text);
        AssertGlobalErrorState(false);

        // If the ErrorContent throws during rendering, it gets caught by the "infinite error loop" detection logic and is fatal
        container.FindElement(By.ClassName("throw-in-errorcontent")).Click();
        AssertGlobalErrorState(true);
    }

    [Fact]
    public void CanHandleErrorsAfterDisposingComponent()
    {
        var container = Browser.Exists(By.Id("error-after-disposal-test"));

        container.FindElement(By.ClassName("throw-after-disposing-component")).Click();
        Browser.Collection(() => container.FindElements(By.ClassName("received-exception")),
            elem => Assert.Equal("Delayed asynchronous exception in OnParametersSetAsync", elem.Text));

        AssertGlobalErrorState(false);
    }

    [Fact]
    public async Task CanHandleErrorsAfterDisposingErrorBoundary()
    {
        var container = Browser.Exists(By.Id("error-after-disposal-test"));
        container.FindElement(By.ClassName("throw-after-disposing-errorboundary")).Click();

        // Because we've actually removed the error boundary, there isn't any UI for us to assert about.
        // The following delay is a cheap way to check for that - in the worst case, we could get a false
        // test pass here if the delay is somehow not long enough, but this should never lead to a false
        // failure (i.e., flakiness).
        await Task.Delay(1000); // The test exception occurs after 500ms

        // We succeed as long as there's no global error and the rest of the UI is still there
        Browser.Exists(By.Id("error-after-disposal-test"));
        AssertGlobalErrorState(false);
    }

    [Fact]
    public void CanHandleMultipleAsyncErrorsFromDescendants()
    {
        var container = Browser.Exists(By.Id("multiple-child-errors-test"));
        var message = "Delayed asynchronous exception in OnParametersSetAsync";

        container.FindElement(By.ClassName("throw-in-children")).Click();
        Browser.Collection(() => container.FindElements(By.ClassName("received-exception")),
            elem => Assert.Equal(message, elem.Text),
            elem => Assert.Equal(message, elem.Text),
            elem => Assert.Equal(message, elem.Text));

        AssertGlobalErrorState(false);
    }

    void AssertGlobalErrorState(bool hasGlobalError)
    {
        var globalErrorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Browser.Equal(hasGlobalError ? "block" : "none", () => globalErrorUi.GetCssValue("display"));
    }
}
