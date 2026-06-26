// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

// Async form validation E2E tests. These run in both interactive modes via the concrete subclasses.
// The components and validators are interactive, so the pending/faulted/supersession UX is exercised in both modes.
public abstract class AsyncFormValidationTestBase : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public AsyncFormValidationTestBase(
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
    public void AsyncFieldValidation_ShowsPendingThenSettlesValid()
    {
        // Editing a field starts a per-field async validation that parks the field in the pending
        // state until it settles. The gate keeps it pending until the test releases it.
        var appElement = Browser.MountTestComponent<AsyncValidationComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));

        userNameInput.SendKeys("available\t");

        Browser.Exists(By.Id("username-pending"));

        appElement.FindElement(By.Id("release-field")).Click();

        Browser.DoesNotExist(By.Id("username-pending"));
        Browser.DoesNotExist(By.Id("username-faulted"));
        Browser.Empty(() => appElement.FindElements(By.ClassName("username-message")));
    }

    [Fact]
    public void AsyncFieldValidation_ThrowingValidator_ShowsPendingThenFaulted()
    {
        // A per-field validator that throws is contained as a faulted field, observable via
        // IsValidationFaulted rather than as a validation message.
        var appElement = Browser.MountTestComponent<AsyncValidationComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));

        userNameInput.SendKeys("error\t");

        Browser.Exists(By.Id("username-pending"));

        appElement.FindElement(By.Id("release-field")).Click();

        Browser.Exists(By.Id("username-faulted"));
        Browser.DoesNotExist(By.Id("username-pending"));
        Browser.Empty(() => appElement.FindElements(By.ClassName("username-message")));
    }

    [Fact]
    public void AsyncFieldValidation_ReeditWhilePending_SupersedesPriorValidation()
    {
        // Re-editing a field while its validation is still pending supersedes the prior validation.
        // Only the latest result wins and the field is never left stuck in the pending state.
        var appElement = Browser.MountTestComponent<AsyncValidationComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));

        // First edit would resolve to an invalid result, but it stays pending behind the gate.
        userNameInput.SendKeys("taken\t");
        Browser.Exists(By.Id("username-pending"));

        // Re-edit to a value that resolves valid; this supersedes the prior pending validation.
        userNameInput.Clear();
        userNameInput.SendKeys("available\t");
        Browser.Exists(By.Id("username-pending"));

        appElement.FindElement(By.Id("release-field")).Click();

        // The latest (valid) validation wins: not pending, not faulted, and crucially the superseded
        // "taken" message never appears.
        Browser.DoesNotExist(By.Id("username-pending"));
        Browser.DoesNotExist(By.Id("username-faulted"));
        Browser.Empty(() => appElement.FindElements(By.ClassName("username-message")));
    }

    [Fact]
    public void AsyncFormValidation_OnSubmit_ShowsPendingThenInvokesValidSubmit()
    {
        // Submitting awaits the form-level async validation. While it runs, the form reports
        // pending and the submit button is disabled; OnValidSubmit fires only after it settles valid.
        var appElement = Browser.MountTestComponent<AsyncValidationComponent>();
        var submitButton = appElement.FindElement(By.Id("submit"));

        submitButton.Click();

        Browser.Exists(By.Id("form-pending"));
        Browser.True(() => !appElement.FindElement(By.Id("submit")).Enabled);
        Assert.Empty(appElement.FindElements(By.Id("last-callback")));

        appElement.FindElement(By.Id("release-form")).Click();

        Browser.Equal("OnValidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        Browser.DoesNotExist(By.Id("form-pending"));
    }

    [Fact]
    public void AsyncFormValidation_OnSubmit_CancelsPendingFieldValidation()
    {
        // Submitting supersedes any in-flight per-field validation. The pending field task is
        // cancelled, the field leaves the pending state, and the form-level validation takes over.
        var appElement = Browser.MountTestComponent<AsyncValidationComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));

        userNameInput.SendKeys("available\t");
        Browser.Exists(By.Id("username-pending"));

        appElement.FindElement(By.Id("submit")).Click();

        // The pending field validation is cancelled and the form-level validation is now pending.
        Browser.DoesNotExist(By.Id("username-pending"));
        Browser.Exists(By.Id("form-pending"));

        appElement.FindElement(By.Id("release-form")).Click();

        Browser.Equal("OnValidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        Browser.DoesNotExist(By.Id("form-pending"));
    }

    [Fact]
    public void AsyncDataAnnotationsValidation_ValidatorPath_PerFieldInvalid_ShowsMessage()
    {
        // Exercises the static Validator path (Validator.TryValidatePropertyAsync) for an async
        // [ValidationAttribute] on field change. The model is not registered with MEV.
        var appElement = Browser.MountTestComponent<DataAnnotationsValidatorComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        userNameInput.SendKeys("taken\t");

        Browser.Equal(new[] { "Username is taken" }, messagesAccessor);
    }

    [Fact]
    public void AsyncDataAnnotationsValidation_ValidatorPath_FormLevelAsync_OnSubmit()
    {
        // Exercises the static Validator path (Validator.TryValidateObjectAsync + IAsyncValidatableObject)
        // on submit. "reserved" passes the per-field check but is rejected by the form-level async check.
        var appElement = Browser.MountTestComponent<DataAnnotationsValidatorComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        userNameInput.SendKeys("reserved\t");
        appElement.FindElement(By.Id("submit")).Click();

        Browser.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        Browser.Equal(new[] { "Username is reserved" }, messagesAccessor);
    }

    [Fact]
    public void AsyncDataAnnotationsValidation_MevPath_PerFieldInvalid_ShowsMessage()
    {
        // Exercises the Microsoft.Extensions.Validation path (IValidatablePropertyInfo.ValidateAsync) for
        // an async [ValidationAttribute] on field change. The model is registered via AsyncValidationResolver.
        var appElement = Browser.MountTestComponent<DataAnnotationsValidatableInfoComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        userNameInput.SendKeys("taken\t");

        Browser.Equal(new[] { "Username is taken" }, messagesAccessor);
    }

    [Fact]
    public void AsyncDataAnnotationsValidation_MevPath_FormLevelAsync_OnSubmit()
    {
        // Exercises the Microsoft.Extensions.Validation path (IValidatableTypeInfo.ValidateAsync +
        // IAsyncValidatableObject) on submit. "reserved" passes the per-field check but is rejected by
        // the form-level async check.
        var appElement = Browser.MountTestComponent<DataAnnotationsValidatableInfoComponent>();
        var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        userNameInput.SendKeys("reserved\t");
        appElement.FindElement(By.Id("submit")).Click();

        Browser.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        Browser.Equal(new[] { "Username is reserved" }, messagesAccessor);
    }

    private Func<string[]> CreateValidationMessagesAccessor(IWebElement appElement, string messageSelector = ".validation-message")
    {
        return () => appElement.FindElements(By.CssSelector(messageSelector))
            .Select(x => x.Text)
            .OrderBy(x => x)
            .ToArray();
    }
}
