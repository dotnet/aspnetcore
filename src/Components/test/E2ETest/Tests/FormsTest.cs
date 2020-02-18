// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class FormsTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public FormsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        }

        protected virtual IWebElement MountSimpleValidationComponent()
            => Browser.MountTestComponent<SimpleValidationComponent>();

        protected virtual IWebElement MountTypicalValidationComponent()
            => Browser.MountTestComponent<TypicalValidationComponent>();

        [Fact]
        public async Task EditFormWorksWithDataAnnotationsValidator()
        {
            var appElement = MountSimpleValidationComponent();
            var form = appElement.FindElement(By.TagName("form"));
            var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // The form emits unmatched attributes
            Browser.Equal("off", () => form.GetAttribute("autocomplete"));

            // Editing a field doesn't trigger validation on its own
            userNameInput.SendKeys("Bert\t");
            acceptsTermsInput.Click(); // Accept terms
            acceptsTermsInput.Click(); // Un-accept terms
            await Task.Delay(500); // There's no expected change to the UI, so just wait a moment before asserting
            Browser.Empty(messagesAccessor);
            Assert.Empty(appElement.FindElements(By.Id("last-callback")));

            // Submitting the form does validate
            submitButton.Click();
            Browser.Equal(new[] { "You must accept the terms" }, messagesAccessor);
            Browser.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);

            // Can make another field invalid
            userNameInput.Clear();
            submitButton.Click();
            Browser.Equal(new[] { "Please choose a username", "You must accept the terms" }, messagesAccessor);
            Browser.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);

            // Can make valid
            userNameInput.SendKeys("Bert\t");
            acceptsTermsInput.Click();
            submitButton.Click();
            Browser.Empty(messagesAccessor);
            Browser.Equal("OnValidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        }

        [Fact]
        public void InputTextInteractsWithEditContext()
        {
            var appElement = MountTypicalValidationComponent();
            var nameInput = appElement.FindElement(By.ClassName("name")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // InputText emits unmatched attributes
            Browser.Equal("Enter your name", () => nameInput.GetAttribute("placeholder"));

            // Validates on edit
            Browser.Equal("valid", () => nameInput.GetAttribute("class"));
            nameInput.SendKeys("Bert\t");
            Browser.Equal("modified valid", () => nameInput.GetAttribute("class"));

            // Can become invalid
            nameInput.SendKeys("01234567890123456789\t");
            Browser.Equal("modified invalid", () => nameInput.GetAttribute("class"));
            Browser.Equal(new[] { "That name is too long" }, messagesAccessor);

            // Can become valid
            nameInput.Clear();
            nameInput.SendKeys("Bert\t");
            Browser.Equal("modified valid", () => nameInput.GetAttribute("class"));
            Browser.Empty(messagesAccessor);
        }

        [Fact]
        public void InputNumberInteractsWithEditContext_NonNullableInt()
        {
            var appElement = MountTypicalValidationComponent();
            var ageInput = appElement.FindElement(By.ClassName("age")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // InputNumber emits unmatched attributes
            Browser.Equal("Enter your age", () => ageInput.GetAttribute("placeholder"));

            // Validates on edit
            Browser.Equal("valid", () => ageInput.GetAttribute("class"));
            ageInput.SendKeys("123\t");
            Browser.Equal("modified valid", () => ageInput.GetAttribute("class"));

            // Can become invalid
            ageInput.SendKeys("e100\t");
            Browser.Equal("modified invalid", () => ageInput.GetAttribute("class"));
            Browser.Equal(new[] { "The AgeInYears field must be a number." }, messagesAccessor);

            // Empty is invalid, because it's not a nullable int
            ageInput.Clear();
            ageInput.SendKeys("\t");
            Browser.Equal("modified invalid", () => ageInput.GetAttribute("class"));
            Browser.Equal(new[] { "The AgeInYears field must be a number." }, messagesAccessor);

            // Zero is within the allowed range
            ageInput.SendKeys("0\t");
            Browser.Equal("modified valid", () => ageInput.GetAttribute("class"));
            Browser.Empty(messagesAccessor);
        }

        [Fact]
        public void InputNumberInteractsWithEditContext_NullableFloat()
        {
            var appElement = MountTypicalValidationComponent();
            var heightInput = appElement.FindElement(By.ClassName("height")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            Browser.Equal("valid", () => heightInput.GetAttribute("class"));
            heightInput.SendKeys("123.456\t");
            Browser.Equal("modified valid", () => heightInput.GetAttribute("class"));

            // Can become invalid
            heightInput.SendKeys("e100\t");
            Browser.Equal("modified invalid", () => heightInput.GetAttribute("class"));
            Browser.Equal(new[] { "The OptionalHeight field must be a number." }, messagesAccessor);

            // Empty is valid, because it's a nullable float
            heightInput.Clear();
            heightInput.SendKeys("\t");
            Browser.Equal("modified valid", () => heightInput.GetAttribute("class"));
            Browser.Empty(messagesAccessor);
        }

        [Fact]
        public void InputTextAreaInteractsWithEditContext()
        {
            var appElement = MountTypicalValidationComponent();
            var descriptionInput = appElement.FindElement(By.ClassName("description")).FindElement(By.TagName("textarea"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // InputTextArea emits unmatched attributes
            Browser.Equal("Tell us about yourself", () => descriptionInput.GetAttribute("placeholder"));

            // Validates on edit
            Browser.Equal("valid", () => descriptionInput.GetAttribute("class"));
            descriptionInput.SendKeys("Hello\t");
            Browser.Equal("modified valid", () => descriptionInput.GetAttribute("class"));

            // Can become invalid
            descriptionInput.SendKeys("too long too long too long too long too long\t");
            Browser.Equal("modified invalid", () => descriptionInput.GetAttribute("class"));
            Browser.Equal(new[] { "Description is max 20 chars" }, messagesAccessor);

            // Can become valid
            descriptionInput.Clear();
            descriptionInput.SendKeys("Hello\t");
            Browser.Equal("modified valid", () => descriptionInput.GetAttribute("class"));
            Browser.Empty(messagesAccessor);
        }

        [Fact]
        public void InputDateInteractsWithEditContext_NonNullableDateTime()
        {
            var appElement = MountTypicalValidationComponent();
            var renewalDateInput = appElement.FindElement(By.ClassName("renewal-date")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // InputDate emits unmatched attributes
            Browser.Equal("Enter the date", () => renewalDateInput.GetAttribute("placeholder"));

            // Validates on edit
            Browser.Equal("valid", () => renewalDateInput.GetAttribute("class"));
            renewalDateInput.ReplaceText("01/01/2000\t");
            Browser.Equal("modified valid", () => renewalDateInput.GetAttribute("class"));

            // Can become invalid
            ApplyInvalidInputDateValue(".renewal-date input", "11111-11-11");
            Browser.Equal("modified invalid", () => renewalDateInput.GetAttribute("class"));
            Browser.Equal(new[] { "The RenewalDate field must be a date." }, messagesAccessor);

            // Empty is invalid, because it's not nullable
            renewalDateInput.ReplaceText($"{Keys.Backspace}");
            Browser.Equal("modified invalid", () => renewalDateInput.GetAttribute("class"));
            Browser.Equal(new[] { "The RenewalDate field must be a date." }, messagesAccessor);

            // Can become valid
            renewalDateInput.ReplaceText("01/01/01");
            Browser.Equal("modified valid", () => renewalDateInput.GetAttribute("class"));
            Browser.Empty(messagesAccessor);
        }

        [Fact]
        public void InputDateInteractsWithEditContext_NullableDateTimeOffset()
        {
            var appElement = MountTypicalValidationComponent();
            var expiryDateInput = appElement.FindElement(By.ClassName("expiry-date")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            Browser.Equal("valid", () => expiryDateInput.GetAttribute("class"));
            expiryDateInput.SendKeys("01/01/2000\t");
            Browser.Equal("modified valid", () => expiryDateInput.GetAttribute("class"));

            // Can become invalid
            ApplyInvalidInputDateValue(".expiry-date input", "11111-11-11");
            Browser.Equal("modified invalid", () => expiryDateInput.GetAttribute("class"));
            Browser.Equal(new[] { "The OptionalExpiryDate field must be a date." }, messagesAccessor);

            // Empty is valid, because it's nullable
            expiryDateInput.SendKeys($"{Keys.Backspace}\t{Keys.Backspace}\t{Keys.Backspace}\t");
            Browser.Equal("modified valid", () => expiryDateInput.GetAttribute("class"));
            Browser.Empty(messagesAccessor);
        }

        [Fact]
        public void InputSelectInteractsWithEditContext()
        {
            var appElement = MountTypicalValidationComponent();
            var ticketClassInput = new SelectElement(appElement.FindElement(By.ClassName("ticket-class")).FindElement(By.TagName("select")));
            var select = ticketClassInput.WrappedElement;
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // InputSelect emits unmatched attributes
            Browser.Equal("4", () => select.GetAttribute("size"));

            // Validates on edit
            Browser.Equal("valid", () => select.GetAttribute("class"));
            ticketClassInput.SelectByText("First class");
            Browser.Equal("modified valid", () => select.GetAttribute("class"));

            // Can become invalid
            ticketClassInput.SelectByText("(select)");
            Browser.Equal("modified invalid", () => select.GetAttribute("class"));
            Browser.Equal(new[] { "The TicketClass field is not valid." }, messagesAccessor);
        }

        [Fact]
        public void InputCheckboxInteractsWithEditContext()
        {
            var appElement = MountTypicalValidationComponent();
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var isEvilInput = appElement.FindElement(By.ClassName("is-evil")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // InputCheckbox emits unmatched attributes
            Browser.Equal("You have to check this", () => acceptsTermsInput.GetAttribute("title"));

            // Correct initial checkedness
            Assert.False(acceptsTermsInput.Selected);
            Assert.True(isEvilInput.Selected);

            // Validates on edit
            Browser.Equal("valid", () => acceptsTermsInput.GetAttribute("class"));
            Browser.Equal("valid", () => isEvilInput.GetAttribute("class"));
            acceptsTermsInput.Click();
            isEvilInput.Click();
            Browser.Equal("modified valid", () => acceptsTermsInput.GetAttribute("class"));
            Browser.Equal("modified valid", () => isEvilInput.GetAttribute("class"));

            // Can become invalid
            acceptsTermsInput.Click();
            isEvilInput.Click();
            Browser.Equal("modified invalid", () => acceptsTermsInput.GetAttribute("class"));
            Browser.Equal("modified invalid", () => isEvilInput.GetAttribute("class"));
            Browser.Equal(new[] { "Must accept terms", "Must not be evil" }, messagesAccessor);
        }

        [Fact]
        public void CanWireUpINotifyPropertyChangedToEditContext()
        {
            var appElement = Browser.MountTestComponent<NotifyPropertyChangedValidationComponent>();
            var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);
            var submissionStatus = appElement.FindElement(By.Id("submission-status"));

            // Editing a field triggers validation immediately
            Browser.Equal("valid", () => userNameInput.GetAttribute("class"));
            userNameInput.SendKeys("Too long too long\t");
            Browser.Equal("modified invalid", () => userNameInput.GetAttribute("class"));
            Browser.Equal(new[] { "That name is too long" }, messagesAccessor);

            // Submitting the form validates remaining fields
            submitButton.Click();
            Browser.Equal(new[] { "That name is too long", "You must accept the terms" }, messagesAccessor);
            Browser.Equal("modified invalid", () => userNameInput.GetAttribute("class"));
            Browser.Equal("invalid", () => acceptsTermsInput.GetAttribute("class"));

            // Can make fields valid
            userNameInput.Clear();
            userNameInput.SendKeys("Bert\t");
            Browser.Equal("modified valid", () => userNameInput.GetAttribute("class"));
            acceptsTermsInput.Click();
            Browser.Equal("modified valid", () => acceptsTermsInput.GetAttribute("class"));
            Browser.Equal(string.Empty, () => submissionStatus.Text);
            submitButton.Click();
            Browser.True(() => submissionStatus.Text.StartsWith("Submitted"));

            // Fields can revert to unmodified
            Browser.Equal("valid", () => userNameInput.GetAttribute("class"));
            Browser.Equal("valid", () => acceptsTermsInput.GetAttribute("class"));
        }

        [Fact]
        public void ValidationMessageDisplaysMessagesForField()
        {
            var appElement = MountTypicalValidationComponent();
            var emailContainer = appElement.FindElement(By.ClassName("email"));
            var emailInput = emailContainer.FindElement(By.TagName("input"));
            var emailMessagesAccessor = CreateValidationMessagesAccessor(emailContainer);
            var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));

            // Doesn't show messages for other fields
            submitButton.Click();
            Browser.Empty(emailMessagesAccessor);

            // Updates on edit
            emailInput.SendKeys("abc\t");
            Browser.Equal(new[] { "That doesn't look like a real email address" }, emailMessagesAccessor);

            // Can show more than one message
            emailInput.SendKeys("too long too long too long\t");
            Browser.Equal(new[] { "That doesn't look like a real email address", "We only accept very short email addresses (max 10 chars)" }, emailMessagesAccessor);

            // Can become valid
            emailInput.Clear();
            emailInput.SendKeys("a@b.com\t");
            Browser.Empty(emailMessagesAccessor);
        }

        [Fact]
        public void ErrorsFromCompareAttribute()
        {
            var appElement = MountTypicalValidationComponent();
            var emailContainer = appElement.FindElement(By.ClassName("email"));
            var emailInput = emailContainer.FindElement(By.TagName("input"));
            var confirmEmailContainer = appElement.FindElement(By.ClassName("confirm-email"));
            var confirmInput = confirmEmailContainer.FindElement(By.TagName("input"));
            var confirmEmailValidationMessage = CreateValidationMessagesAccessor(confirmEmailContainer);
            var modelErrors = CreateValidationMessagesAccessor(appElement.FindElement(By.ClassName("model-errors")));
            CreateValidationMessagesAccessor(emailContainer);
            var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));

            // Updates on edit
            emailInput.SendKeys("a@b.com\t");

            submitButton.Click();
            Browser.Empty(confirmEmailValidationMessage);
            Browser.Equal(new[] { "Email and confirm email do not match." }, modelErrors);

            confirmInput.SendKeys("not-test@example.com\t");
            Browser.Equal(new[] { "Email and confirm email do not match." }, confirmEmailValidationMessage);

            // Can become correct
            confirmInput.Clear();
            confirmInput.SendKeys("a@b.com\t");

            Browser.Empty(confirmEmailValidationMessage);

            submitButton.Click();
            Browser.Empty(modelErrors);
        }

        [Fact]
        public void InputComponentsCauseContainerToRerenderOnChange()
        {
            var appElement = MountTypicalValidationComponent();
            var ticketClassInput = new SelectElement(appElement.FindElement(By.ClassName("ticket-class")).FindElement(By.TagName("select")));
            var selectedTicketClassDisplay = appElement.FindElement(By.Id("selected-ticket-class"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Shows initial state
            Browser.Equal("Economy", () => selectedTicketClassDisplay.Text);

            // Refreshes on edit
            ticketClassInput.SelectByValue("Premium");
            Browser.Equal("Premium", () => selectedTicketClassDisplay.Text);

            // Leaves previous value unchanged if new entry is unparseable
            ticketClassInput.SelectByText("(select)");
            Browser.Equal(new[] { "The TicketClass field is not valid." }, messagesAccessor);
            Browser.Equal("Premium", () => selectedTicketClassDisplay.Text);
        }

        [Fact]
        public void InputComponentsRespondToAsynchronouslyAddedMessages()
        {
            var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
            var input = appElement.FindElement(By.CssSelector(".username input"));
            var triggerAsyncErrorButton = appElement.FindElement(By.CssSelector(".username button"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Initially shows no error
            Browser.Empty(() => messagesAccessor());
            Browser.Equal("valid", () => input.GetAttribute("class"));

            // Can trigger async error
            triggerAsyncErrorButton.Click();
            Browser.Equal(new[] { "This is invalid, asynchronously" }, messagesAccessor);
            Browser.Equal("invalid", () => input.GetAttribute("class"));
        }

        private Func<string[]> CreateValidationMessagesAccessor(IWebElement appElement)
        {
            return () => appElement.FindElements(By.ClassName("validation-message"))
                .Select(x => x.Text)
                .OrderBy(x => x)
                .ToArray();
        }

        private void ApplyInvalidInputDateValue(string cssSelector, string invalidValue)
        {
            // It's very difficult to enter an invalid value into an <input type=date>, because
            // most combinations of keystrokes get normalized to something valid. Additionally,
            // using Selenium's SendKeys interacts unpredictably with this normalization logic,
            // most likely based on timings. As a workaround, use JS to apply the values. This
            // should only be used when strictly necessary, as it doesn't represent actual user
            // interaction as authentically as SendKeys in other cases.
            var javascript = (IJavaScriptExecutor)Browser;
            javascript.ExecuteScript(
                $"var elem = document.querySelector('{cssSelector}');"
                + $"elem.value = {JsonSerializer.Serialize(invalidValue, TestJsonSerializerOptionsProvider.Options)};"
                + "elem.dispatchEvent(new KeyboardEvent('change'));");
        }
    }
}
