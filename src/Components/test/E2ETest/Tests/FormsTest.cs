// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class FormsTest : BasicTestAppTestBase
    {
        public FormsTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            // On WebAssembly, page reloads are expensive so skip if possible
            Navigate(ServerPathBase, noReload: !serverFixture.UsingAspNetHost);
        }

        [Fact]
        public async Task EditFormWorksWithDataAnnotationsValidator()
        {
            var appElement = MountTestComponent<SimpleValidationComponent>();
            var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var submitButton = appElement.FindElement(By.TagName("button"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Editing a field doesn't trigger validation on its own
            userNameInput.SendKeys("Bert\t");
            acceptsTermsInput.Click(); // Accept terms
            acceptsTermsInput.Click(); // Un-accept terms
            await Task.Delay(500); // There's no expected change to the UI, so just wait a moment before asserting
            WaitAssert.Empty(messagesAccessor);
            Assert.Empty(appElement.FindElements(By.Id("last-callback")));

            // Submitting the form does validate
            submitButton.Click();
            WaitAssert.Equal(new[] { "You must accept the terms" }, messagesAccessor);
            WaitAssert.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);

            // Can make another field invalid
            userNameInput.Clear();
            submitButton.Click();
            WaitAssert.Equal(new[] { "Please choose a username", "You must accept the terms" }, messagesAccessor);
            WaitAssert.Equal("OnInvalidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);

            // Can make valid
            userNameInput.SendKeys("Bert\t");
            acceptsTermsInput.Click();
            submitButton.Click();
            WaitAssert.Empty(messagesAccessor);
            WaitAssert.Equal("OnValidSubmit", () => appElement.FindElement(By.Id("last-callback")).Text);
        }

        [Fact]
        public void InputTextInteractsWithEditContext()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var nameInput = appElement.FindElement(By.ClassName("name")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);
            
            // Validates on edit
            WaitAssert.Equal("valid", () => nameInput.GetAttribute("class"));
            nameInput.SendKeys("Bert\t");
            WaitAssert.Equal("modified valid", () => nameInput.GetAttribute("class"));

            // Can become invalid
            nameInput.SendKeys("01234567890123456789\t");
            WaitAssert.Equal("modified invalid", () => nameInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "That name is too long" }, messagesAccessor);

            // Can become valid
            nameInput.Clear();
            nameInput.SendKeys("Bert\t");
            WaitAssert.Equal("modified valid", () => nameInput.GetAttribute("class"));
            WaitAssert.Empty(messagesAccessor);
        }

        [Fact]
        public void InputNumberInteractsWithEditContext_NonNullableInt()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var ageInput = appElement.FindElement(By.ClassName("age")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => ageInput.GetAttribute("class"));
            ageInput.SendKeys("123\t");
            WaitAssert.Equal("modified valid", () => ageInput.GetAttribute("class"));

            // Can become invalid
            ageInput.SendKeys("e100\t");
            WaitAssert.Equal("modified invalid", () => ageInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The AgeInYears field must be a number." }, messagesAccessor);

            // Empty is invalid, because it's not a nullable int
            ageInput.Clear();
            ageInput.SendKeys("\t");
            WaitAssert.Equal("modified invalid", () => ageInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The AgeInYears field must be a number." }, messagesAccessor);

            // Zero is within the allowed range
            ageInput.SendKeys("0\t");
            WaitAssert.Equal("modified valid", () => ageInput.GetAttribute("class"));
            WaitAssert.Empty(messagesAccessor);
        }

        [Fact]
        public void InputNumberInteractsWithEditContext_NullableFloat()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var heightInput = appElement.FindElement(By.ClassName("height")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => heightInput.GetAttribute("class"));
            heightInput.SendKeys("123.456\t");
            WaitAssert.Equal("modified valid", () => heightInput.GetAttribute("class"));

            // Can become invalid
            heightInput.SendKeys("e100\t");
            WaitAssert.Equal("modified invalid", () => heightInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The OptionalHeight field must be a number." }, messagesAccessor);

            // Empty is valid, because it's a nullable float
            heightInput.Clear();
            heightInput.SendKeys("\t");
            WaitAssert.Equal("modified valid", () => heightInput.GetAttribute("class"));
            WaitAssert.Empty(messagesAccessor);
        }

        [Fact]
        public void InputTextAreaInteractsWithEditContext()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var descriptionInput = appElement.FindElement(By.ClassName("description")).FindElement(By.TagName("textarea"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => descriptionInput.GetAttribute("class"));
            descriptionInput.SendKeys("Hello\t");
            WaitAssert.Equal("modified valid", () => descriptionInput.GetAttribute("class"));

            // Can become invalid
            descriptionInput.SendKeys("too long too long too long too long too long\t");
            WaitAssert.Equal("modified invalid", () => descriptionInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "Description is max 20 chars" }, messagesAccessor);

            // Can become valid
            descriptionInput.Clear();
            descriptionInput.SendKeys("Hello\t");
            WaitAssert.Equal("modified valid", () => descriptionInput.GetAttribute("class"));
            WaitAssert.Empty(messagesAccessor);
        }

        [Fact]
        public void InputDateInteractsWithEditContext_NonNullableDateTime()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var renewalDateInput = appElement.FindElement(By.ClassName("renewal-date")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => renewalDateInput.GetAttribute("class"));
            renewalDateInput.SendKeys("01/01/2000\t");
            WaitAssert.Equal("modified valid", () => renewalDateInput.GetAttribute("class"));

            // Can become invalid
            renewalDateInput.SendKeys("0/0/0");
            WaitAssert.Equal("modified invalid", () => renewalDateInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The RenewalDate field must be a date." }, messagesAccessor);

            // Empty is invalid, because it's not nullable
            renewalDateInput.SendKeys($"{Keys.Backspace}\t{Keys.Backspace}\t{Keys.Backspace}\t");
            WaitAssert.Equal("modified invalid", () => renewalDateInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The RenewalDate field must be a date." }, messagesAccessor);

            // Can become valid
            renewalDateInput.SendKeys("01/01/01\t");
            WaitAssert.Equal("modified valid", () => renewalDateInput.GetAttribute("class"));
            WaitAssert.Empty(messagesAccessor);
        }

        [Fact]
        public void InputDateInteractsWithEditContext_NullableDateTimeOffset()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var expiryDateInput = appElement.FindElement(By.ClassName("expiry-date")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => expiryDateInput.GetAttribute("class"));
            expiryDateInput.SendKeys("01/01/2000\t");
            WaitAssert.Equal("modified valid", () => expiryDateInput.GetAttribute("class"));

            // Can become invalid
            expiryDateInput.SendKeys("111111111");
            WaitAssert.Equal("modified invalid", () => expiryDateInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The OptionalExpiryDate field must be a date." }, messagesAccessor);

            // Empty is valid, because it's nullable
            expiryDateInput.SendKeys($"{Keys.Backspace}\t{Keys.Backspace}\t{Keys.Backspace}\t");
            WaitAssert.Equal("modified valid", () => expiryDateInput.GetAttribute("class"));
            WaitAssert.Empty(messagesAccessor);
        }

        [Fact]
        public void InputSelectInteractsWithEditContext()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var ticketClassInput = new SelectElement(appElement.FindElement(By.ClassName("ticket-class")).FindElement(By.TagName("select")));
            var select = ticketClassInput.WrappedElement;
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => select.GetAttribute("class"));
            ticketClassInput.SelectByText("First class");
            WaitAssert.Equal("modified valid", () => select.GetAttribute("class"));

            // Can become invalid
            ticketClassInput.SelectByText("(select)");
            WaitAssert.Equal("modified invalid", () => select.GetAttribute("class"));
            WaitAssert.Equal(new[] { "The TicketClass field is not valid." }, messagesAccessor);
        }

        [Fact]
        public void InputCheckboxInteractsWithEditContext()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Validates on edit
            WaitAssert.Equal("valid", () => acceptsTermsInput.GetAttribute("class"));
            acceptsTermsInput.Click();
            WaitAssert.Equal("modified valid", () => acceptsTermsInput.GetAttribute("class"));

            // Can become invalid
            acceptsTermsInput.Click();
            WaitAssert.Equal("modified invalid", () => acceptsTermsInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "Must accept terms" }, messagesAccessor);
        }

        [Fact]
        public void CanWireUpINotifyPropertyChangedToEditContext()
        {
            var appElement = MountTestComponent<NotifyPropertyChangedValidationComponent>();
            var userNameInput = appElement.FindElement(By.ClassName("user-name")).FindElement(By.TagName("input"));
            var acceptsTermsInput = appElement.FindElement(By.ClassName("accepts-terms")).FindElement(By.TagName("input"));
            var submitButton = appElement.FindElement(By.TagName("button"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);
            var submissionStatus = appElement.FindElement(By.Id("submission-status"));

            // Editing a field triggers validation immediately
            WaitAssert.Equal("valid", () => userNameInput.GetAttribute("class"));
            userNameInput.SendKeys("Too long too long\t");
            WaitAssert.Equal("modified invalid", () => userNameInput.GetAttribute("class"));
            WaitAssert.Equal(new[] { "That name is too long" }, messagesAccessor);

            // Submitting the form validates remaining fields
            submitButton.Click();
            WaitAssert.Equal(new[] { "That name is too long", "You must accept the terms" }, messagesAccessor);
            WaitAssert.Equal("modified invalid", () => userNameInput.GetAttribute("class"));
            WaitAssert.Equal("invalid", () => acceptsTermsInput.GetAttribute("class"));

            // Can make fields valid
            userNameInput.Clear();
            userNameInput.SendKeys("Bert\t");
            WaitAssert.Equal("modified valid", () => userNameInput.GetAttribute("class"));
            acceptsTermsInput.Click();
            WaitAssert.Equal("modified valid", () => acceptsTermsInput.GetAttribute("class"));
            WaitAssert.Equal(string.Empty, () => submissionStatus.Text);
            submitButton.Click();
            WaitAssert.True(() => submissionStatus.Text.StartsWith("Submitted"));

            // Fields can revert to unmodified
            WaitAssert.Equal("valid", () => userNameInput.GetAttribute("class"));
            WaitAssert.Equal("valid", () => acceptsTermsInput.GetAttribute("class"));
        }

        [Fact]
        public void ValidationMessageDisplaysMessagesForField()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var emailContainer = appElement.FindElement(By.ClassName("email"));
            var emailInput = emailContainer.FindElement(By.TagName("input"));
            var emailMessagesAccessor = CreateValidationMessagesAccessor(emailContainer);
            var submitButton = appElement.FindElement(By.TagName("button"));

            // Doesn't show messages for other fields
            submitButton.Click();
            WaitAssert.Empty(emailMessagesAccessor);

            // Updates on edit
            emailInput.SendKeys("abc\t");
            WaitAssert.Equal(new[] { "That doesn't look like a real email address" }, emailMessagesAccessor);

            // Can show more than one message
            emailInput.SendKeys("too long too long too long\t");
            WaitAssert.Equal(new[] { "That doesn't look like a real email address", "We only accept very short email addresses (max 10 chars)" }, emailMessagesAccessor);

            // Can become valid
            emailInput.Clear();
            emailInput.SendKeys("a@b.com\t");
            WaitAssert.Empty(emailMessagesAccessor);
        }

        [Fact]
        public void InputComponentsCauseContainerToRerenderOnChange()
        {
            var appElement = MountTestComponent<TypicalValidationComponent>();
            var ticketClassInput = new SelectElement(appElement.FindElement(By.ClassName("ticket-class")).FindElement(By.TagName("select")));
            var selectedTicketClassDisplay = appElement.FindElement(By.Id("selected-ticket-class"));
            var messagesAccessor = CreateValidationMessagesAccessor(appElement);

            // Shows initial state
            WaitAssert.Equal("Economy", () => selectedTicketClassDisplay.Text);

            // Refreshes on edit
            ticketClassInput.SelectByValue("Premium");
            WaitAssert.Equal("Premium", () => selectedTicketClassDisplay.Text);

            // Leaves previous value unchanged if new entry is unparseable
            ticketClassInput.SelectByText("(select)");
            WaitAssert.Equal(new[] { "The TicketClass field is not valid." }, messagesAccessor);
            WaitAssert.Equal("Premium", () => selectedTicketClassDisplay.Text);
        }

        private Func<string[]> CreateValidationMessagesAccessor(IWebElement appElement)
        {
            return () => appElement.FindElements(By.ClassName("validation-message"))
                .Select(x => x.Text)
                .OrderBy(x => x)
                .ToArray();
        }
    }
}
