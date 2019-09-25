// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class FormsTestWithExperimentalValidator : FormsTest
    {
        public FormsTestWithExperimentalValidator(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<BasicTestApp.Program> serverFixture,
            ITestOutputHelper output) : base(browserFixture, serverFixture, output)
        {
        }

        protected override IWebElement MountSimpleValidationComponent()
            => Browser.MountTestComponent<SimpleValidationComponentUsingExperimentalValidator>();

        protected override IWebElement MountTypicalValidationComponent()
            => Browser.MountTestComponent<TypicalValidationComponentUsingExperimentalValidator>();

        [Fact]
        public void EditFormWorksWithNestedValidation()
        {
            var appElement = Browser.MountTestComponent<ExperimentalValidationComponent>();

            var nameInput = appElement.FindElement(By.CssSelector(".name input"));
            var emailInput = appElement.FindElement(By.CssSelector(".email input"));
            var confirmEmailInput = appElement.FindElement(By.CssSelector(".confirm-email input"));
            var streetInput = appElement.FindElement(By.CssSelector(".street input"));
            var zipInput = appElement.FindElement(By.CssSelector(".zip input"));
            var countryInput = new SelectElement(appElement.FindElement(By.CssSelector(".country select")));
            var descriptionInput = appElement.FindElement(By.CssSelector(".description input"));
            var weightInput = appElement.FindElement(By.CssSelector(".weight input"));

            var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));

            submitButton.Click();

            Browser.Equal(4, () => appElement.FindElements(By.CssSelector(".all-errors .validation-message")).Count);

            Browser.Equal("Enter a name", () => appElement.FindElement(By.CssSelector(".name .validation-message")).Text);
            Browser.Equal("Enter an email", () => appElement.FindElement(By.CssSelector(".email .validation-message")).Text);
            Browser.Equal("A street address is required.", () => appElement.FindElement(By.CssSelector(".street .validation-message")).Text);
            Browser.Equal("Description is required.", () => appElement.FindElement(By.CssSelector(".description .validation-message")).Text);

            // Verify class-level validation
            nameInput.SendKeys("Some person");
            emailInput.SendKeys("test@example.com");
            countryInput.SelectByValue("Mordor");
            descriptionInput.SendKeys("Fragile staff");
            streetInput.SendKeys("Mount Doom\t");

            submitButton.Click();

            // Verify member validation from IValidatableObject on a model property, CustomValidationAttribute on a model attribute, and BlazorCompareAttribute.
            Browser.Equal("A ZipCode is required", () => appElement.FindElement(By.CssSelector(".zip .validation-message")).Text);
            Browser.Equal("'Confirm email address' and 'EmailAddress' do not match.", () => appElement.FindElement(By.CssSelector(".confirm-email .validation-message")).Text);
            Browser.Equal("Fragile items must be placed in secure containers", () => appElement.FindElement(By.CssSelector(".item-error .validation-message")).Text);
            Browser.Equal(3, () => appElement.FindElements(By.CssSelector(".all-errors .validation-message")).Count);

            zipInput.SendKeys("98052");
            confirmEmailInput.SendKeys("test@example.com");
            descriptionInput.Clear();
            weightInput.SendKeys("0");
            descriptionInput.SendKeys("The One Ring\t");

            submitButton.Click();
            // Verify validation from IValidatableObject on the model.
            Browser.Equal("Some items in your list cannot be delivered.", () => appElement.FindElement(By.CssSelector(".model-errors .validation-message")).Text);

            Browser.Single(() => appElement.FindElements(By.CssSelector(".all-errors .validation-message")));

            // Let's make sure the form submits
            descriptionInput.Clear();
            descriptionInput.SendKeys("A different ring\t");
            submitButton.Click();

            Browser.Empty(() => appElement.FindElements(By.CssSelector(".all-errors .validation-message")));
            Browser.Equal("OnValidSubmit", () => appElement.FindElement(By.CssSelector(".submission-log")).Text);
        }
    }
}
