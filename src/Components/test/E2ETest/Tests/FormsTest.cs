// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

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
    public void EditFormWorksWithDataAnnotationsValidatorAndDI()
    {
        var appElement = Browser.MountTestComponent<ValidationComponentDI>();
        var form = appElement.FindElement(By.TagName("form"));
        var userNameInput = appElement.FindElement(By.ClassName("the-quiz")).FindElement(By.TagName("input"));
        var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        userNameInput.SendKeys("Bacon\t");
        submitButton.Click();
        //We can only have this errormessage when DI is working
        Browser.Equal(new[] { "You should not put that in a salad!" }, messagesAccessor);

        userNameInput.Clear();
        userNameInput.SendKeys("Watermelon\t");
        submitButton.Click();
        Browser.Empty(messagesAccessor);
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
        EnsureAttributeNotRendered(nameInput, "aria-invalid");

        // Can become invalid
        nameInput.SendKeys("01234567890123456789\t");
        Browser.Equal("modified invalid", () => nameInput.GetAttribute("class"));
        EnsureAttributeValue(nameInput, "aria-invalid", "true");
        Browser.Equal(new[] { "That name is too long" }, messagesAccessor);

        // Can become valid
        nameInput.Clear();
        nameInput.SendKeys("Bert\t");
        Browser.Equal("modified valid", () => nameInput.GetAttribute("class"));
        EnsureAttributeNotRendered(nameInput, "aria-invalid");
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
    public void InputSelectInteractsWithEditContext_BoolValues()
    {
        var appElement = MountTypicalValidationComponent();
        var ticketClassInput = new SelectElement(appElement.FindElement(By.ClassName("select-bool-values")).FindElement(By.TagName("select")));
        var select = ticketClassInput.WrappedElement;
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // Invalidates on edit
        Browser.Equal("valid", () => select.GetAttribute("class"));
        ticketClassInput.SelectByText("true");
        Browser.Equal("modified invalid", () => select.GetAttribute("class"));
        Browser.Equal(new[] { "77 + 33 = 100 is a false statement, unfortunately." }, messagesAccessor);

        // Nullable conversion can fail
        ticketClassInput.SelectByText("(select)");
        Browser.Equal("modified invalid", () => select.GetAttribute("class"));
        Browser.Equal(new[]
        {
                "77 + 33 = 100 is a false statement, unfortunately.",
                "The IsSelectMathStatementTrue field is not valid."
            }, messagesAccessor);

        // Can become valid
        ticketClassInput.SelectByText("false");
        Browser.Equal("modified valid", () => select.GetAttribute("class"));
    }

    [Fact]
    public void InputSelectInteractsWithEditContext_MultipleAttribute()
    {
        var appElement = MountTypicalValidationComponent();
        var citiesInput = new SelectElement(appElement.FindElement(By.ClassName("cities")).FindElement(By.TagName("select")));
        var select = citiesInput.WrappedElement;
        var messagesAccesor = CreateValidationMessagesAccessor(appElement);

        // Binding applies to option selection
        Browser.Equal(new[] { "SanFrancisco" }, () => citiesInput.AllSelectedOptions.Select(option => option.GetAttribute("value")));

        // Validates on edit
        Browser.Equal("valid", () => select.GetAttribute("class"));
        citiesInput.SelectByIndex(2);
        Browser.Equal("modified valid", () => select.GetAttribute("class"));

        // Can become invalid
        citiesInput.SelectByIndex(1);
        citiesInput.SelectByIndex(3);
        Browser.Equal("modified invalid", () => select.GetAttribute("class"));
        Browser.Equal(new[] { "The field SelectedCities must be a string or array type with a maximum length of '3'." }, messagesAccesor);
    }

    [Fact]
    public void InputSelectIgnoresMultipleAttribute()
    {
        var appElement = MountTypicalValidationComponent();
        var ticketClassInput = new SelectElement(appElement.FindElement(By.ClassName("ticket-class")).FindElement(By.TagName("select")));
        var select = ticketClassInput.WrappedElement;

        // Select does not have the 'multiple' attribute
        Browser.False(() => ticketClassInput.IsMultiple);

        // Check initial selection
        Browser.Equal("Economy class", () => ticketClassInput.SelectedOption.Text);

        ticketClassInput.SelectByText("First class");

        // Only one option selected
        Browser.Equal(1, () => ticketClassInput.AllSelectedOptions.Count);
    }

    [Fact]
    public void InputSelectHandlesHostileStringValues()
    {
        var appElement = MountTypicalValidationComponent();
        var selectParagraph = appElement.FindElement(By.ClassName("select-multiple-hostile"));
        var hostileSelectInput = new SelectElement(selectParagraph.FindElement(By.TagName("select")));
        var select = hostileSelectInput.WrappedElement;
        var hostileSelectLabel = selectParagraph.FindElement(By.TagName("span"));

        // Check initial selection
        Browser.Equal(new[] { "\"", "{" }, () => hostileSelectInput.AllSelectedOptions.Select(o => o.Text));

        hostileSelectInput.DeselectByIndex(0);
        hostileSelectInput.SelectByIndex(2);

        // Bindings work from JS -> C#
        Browser.Equal("{,", () => hostileSelectLabel.Text);
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
    public void InputRadioGroupWithoutNameInteractsWithEditContext()
    {
        var appElement = MountTypicalValidationComponent();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // Validate selected inputs
        Browser.True(() => FindUnknownAirlineInput().Selected);
        Browser.False(() => FindBestAirlineInput().Selected);

        // InputRadio emits additional attributes
        Browser.True(() => FindUnknownAirlineInput().GetAttribute("extra").Equals("additional"));

        // Validates on edit
        Browser.Equal("valid", () => FindUnknownAirlineInput().GetAttribute("class"));
        Browser.Equal("valid", () => FindBestAirlineInput().GetAttribute("class"));

        FindBestAirlineInput().Click();

        Browser.Equal("modified valid", () => FindUnknownAirlineInput().GetAttribute("class"));
        Browser.Equal("modified valid", () => FindBestAirlineInput().GetAttribute("class"));

        // Can become invalid
        FindUnknownAirlineInput().Click();

        Browser.Equal("modified invalid", () => FindUnknownAirlineInput().GetAttribute("class"));
        Browser.Equal("modified invalid", () => FindBestAirlineInput().GetAttribute("class"));
        Browser.Equal(new[] { "Pick a valid airline." }, messagesAccessor);

        IReadOnlyCollection<IWebElement> FindAirlineInputs()
            => appElement.FindElement(By.ClassName("airline")).FindElements(By.TagName("input"));

        IWebElement FindUnknownAirlineInput()
            => FindAirlineInputs().First(i => string.Equals("Unknown", i.GetAttribute("value")));

        IWebElement FindBestAirlineInput()
            => FindAirlineInputs().First(i => string.Equals("BestAirline", i.GetAttribute("value")));
    }

    [Fact]
    public void InputRadioGroupsWithNamesNestedInteractWithEditContext()
    {
        var appElement = MountTypicalValidationComponent();
        var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));
        var group = appElement.FindElement(By.ClassName("nested-radio-group"));

        // Validate unselected inputs
        Browser.True(() => FindCountryInputs().All(i => !i.Selected));
        Browser.True(() => FindColorInputs().All(i => !i.Selected));

        // Invalidates on submit
        Browser.True(() => FindCountryInputs().All(i => string.Equals("valid", i.GetAttribute("class"))));
        Browser.True(() => FindColorInputs().All(i => string.Equals("valid", i.GetAttribute("class"))));

        submitButton.Click();

        Browser.True(() => FindCountryInputs().All(i => string.Equals("invalid", i.GetAttribute("class"))));
        Browser.True(() => FindColorInputs().All(i => string.Equals("invalid", i.GetAttribute("class"))));

        // Validates on edit
        FindCountryInputs().First().Click();

        Browser.True(() => FindCountryInputs().All(i => string.Equals("modified valid", i.GetAttribute("class"))));
        Browser.True(() => FindColorInputs().All(i => string.Equals("invalid", i.GetAttribute("class"))));

        FindColorInputs().First().Click();

        Browser.True(() => FindColorInputs().All(i => string.Equals("modified valid", i.GetAttribute("class"))));

        IReadOnlyCollection<IWebElement> FindCountryInputs() => group.FindElements(By.Name("country"));

        IReadOnlyCollection<IWebElement> FindColorInputs() => group.FindElements(By.Name("color"));
    }

    [Fact]
    public void InputRadioGroupWithBoolValuesInteractsWithEditContext()
    {
        var appElement = MountTypicalValidationComponent();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // Validate selected inputs
        Browser.False(() => FindTrueInput().Selected);
        Browser.True(() => FindFalseInput().Selected);

        // Validates on edit
        Browser.Equal("valid", () => FindTrueInput().GetAttribute("class"));
        Browser.Equal("valid", () => FindFalseInput().GetAttribute("class"));

        FindTrueInput().Click();

        Browser.Equal("modified valid", () => FindTrueInput().GetAttribute("class"));
        Browser.Equal("modified valid", () => FindFalseInput().GetAttribute("class"));

        // Can become invalid
        FindFalseInput().Click();

        Browser.Equal("modified invalid", () => FindTrueInput().GetAttribute("class"));
        Browser.Equal("modified invalid", () => FindFalseInput().GetAttribute("class"));
        Browser.Equal(new[] { "7 * 3 = 21 is a true statement." }, messagesAccessor);

        IReadOnlyCollection<IWebElement> FindInputs()
            => appElement.FindElement(By.ClassName("radio-group-bool-values")).FindElements(By.TagName("input"));

        IWebElement FindTrueInput()
            => FindInputs().First(i => string.Equals("True", i.GetAttribute("value")));

        IWebElement FindFalseInput()
            => FindInputs().First(i => string.Equals("False", i.GetAttribute("value")));
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
        Browser.True(() => submissionStatus.Text.StartsWith("Submitted", StringComparison.Ordinal));

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
        CreateValidationMessagesAccessor(emailContainer);
        var submitButton = appElement.FindElement(By.CssSelector("button[type=submit]"));

        // Updates on edit
        emailInput.SendKeys("a@b.com\t");

        submitButton.Click();
        Browser.Equal(new[] { "Email and confirm email do not match." }, confirmEmailValidationMessage);

        confirmInput.SendKeys("not-test@example.com\t");
        Browser.Equal(new[] { "Email and confirm email do not match." }, confirmEmailValidationMessage);

        // Can become correct
        confirmInput.Clear();
        confirmInput.SendKeys("a@b.com\t");

        Browser.Empty(confirmEmailValidationMessage);
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

    [Fact]
    public void SelectComponentSupportsOptionsComponent()
    {
        var appElement = Browser.MountTestComponent<SelectVariantsComponent>();
        var input = appElement.FindElement(By.Id("input-value"));
        var showAdditionalOptionButton = appElement.FindElement(By.Id("show-additional-option"));
        var selectWithComponent = appElement.FindElement(By.Id("select-with-component"));
        var selectWithoutComponent = appElement.FindElement(By.Id("select-without-component"));

        // Select with custom options component and HTML component behave the
        // same when the selectElement.value is provided
        Browser.Equal("B", () => selectWithoutComponent.GetAttribute("value"));
        Browser.Equal("B", () => selectWithComponent.GetAttribute("value"));

        // Reset to a value that doesn't exist
        input.Clear();
        input.SendKeys("D\t");

        // Confirm that both values are cleared
        Browser.Equal("", () => selectWithComponent.GetAttribute("value"));
        Browser.Equal("", () => selectWithoutComponent.GetAttribute("value"));

        // Dynamically showing the fourth option updates the selected value
        showAdditionalOptionButton.Click();

        Browser.Equal("D", () => selectWithComponent.GetAttribute("value"));
        Browser.Equal("D", () => selectWithoutComponent.GetAttribute("value"));

        // Change the value to one that does really doesn't exist
        input.Clear();
        input.SendKeys("F\t");

        Browser.Equal("", () => selectWithComponent.GetAttribute("value"));
        Browser.Equal("", () => selectWithoutComponent.GetAttribute("value"));
    }

    [Fact]
    public void SelectWithMultipleAttributeCanBindValue()
    {
        var appElement = Browser.MountTestComponent<SelectVariantsComponent>();
        var select = new SelectElement(appElement.FindElement(By.Id("select-cities")));

        // Assert that the binding works in the .NET -> JS direction
        Browser.Equal(new[] { "\"sf\"", "\"sea\"" }, () => select.AllSelectedOptions.Select(option => option.GetAttribute("value")));

        select.DeselectByIndex(0);
        select.SelectByIndex(1);
        select.SelectByIndex(2);

        var label = appElement.FindElement(By.Id("selected-cities-label"));

        // Assert that the binding works in the JS -> .NET direction
        Browser.Equal("\"la\", \"pdx\", \"sea\"", () => label.Text);
    }

    [Fact]
    public void SelectWithMultipleAttributeCanUseOnChangedCallback()
    {
        var appElement = Browser.MountTestComponent<SelectVariantsComponent>();
        var select = new SelectElement(appElement.FindElement(By.Id("select-cars")));

        select.SelectByIndex(2);
        select.SelectByIndex(3);

        var label = appElement.FindElement(By.Id("selected-cars-label"));

        // Assert that the callback was invoked and the selected options were correctly passed.
        Browser.Equal("opel, audi", () => label.Text);
    }

    [Fact]
    public void RespectsCustomFieldCssClassProvider()
    {
        var appElement = MountTypicalValidationComponent();
        var socksInput = appElement.FindElement(By.ClassName("socks")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // Validates on edit
        Browser.Equal("valid-socks", () => socksInput.GetAttribute("class"));
        socksInput.SendKeys("Purple\t");
        Browser.Equal("modified valid-socks", () => socksInput.GetAttribute("class"));

        // Can become invalid
        socksInput.SendKeys(" with yellow spots\t");
        Browser.Equal("modified invalid-socks", () => socksInput.GetAttribute("class"));
    }

    [Fact]
    public void NavigateOnSubmitWorks()
    {
        var app = Browser.MountTestComponent<NavigateOnSubmit>();
        var input = app.FindElement(By.Id("text-input"));

        input.SendKeys(Keys.Enter);

        Browser.Equal("Choose...", () => Browser.WaitUntilTestSelectorReady().SelectedOption.Text);
    }

    [Fact]
    public void CanRemoveAndReAddDataAnnotationsSupport()
    {
        var appElement = MountTypicalValidationComponent();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);
        var nameInput = appElement.FindElement(By.ClassName("name")).FindElement(By.TagName("input"));
        Func<string> lastLogEntryAccessor = () => appElement.FindElement(By.CssSelector(".submission-log-entry:last-of-type")).Text;

        nameInput.SendKeys("01234567890123456789\t");
        Browser.Equal("modified invalid", () => nameInput.GetAttribute("class"));
        Browser.Equal(new[] { "That name is too long" }, messagesAccessor);

        // Remove DataAnnotations support
        appElement.FindElement(By.Id("toggle-dataannotations")).Click();
        Browser.Equal("DataAnnotations support is now disabled", lastLogEntryAccessor);
        Browser.Equal("modified valid", () => nameInput.GetAttribute("class"));
        Browser.Empty(messagesAccessor);

        // Re-add DataAnnotations support
        appElement.FindElement(By.Id("toggle-dataannotations")).Click();
        nameInput.SendKeys("0\t");
        Browser.Equal("DataAnnotations support is now enabled", lastLogEntryAccessor);
        Browser.Equal("modified invalid", () => nameInput.GetAttribute("class"));
        Browser.Equal(new[] { "That name is too long" }, messagesAccessor);
    }

    [Fact]
    public void InputRangeAttributeOrderDoesNotAffectValue()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/33499

        var appElement = Browser.MountTestComponent<InputRangeComponent>();
        var rangeWithValueFirst = appElement.FindElement(By.Id("range-value-first"));
        var rangeWithValueLast = appElement.FindElement(By.Id("range-value-last"));

        // Value never gets incorrectly clamped.
        Browser.Equal("210", () => rangeWithValueFirst.GetDomProperty("value"));
        Browser.Equal("210", () => rangeWithValueLast.GetDomProperty("value"));
    }

    [Fact]
    public void InputSelectWorksWithoutEditContext()
    {
        var appElement = Browser.MountTestComponent<InputsWithoutEditForm>();
        var selectElement = new SelectElement(appElement.FindElement(By.Id("selected-cities-input-select")));
        var selectedElementText = appElement.FindElement(By.Id("selected-cities-text"));

        // The bound value is expected and no class attribute exists
        Browser.Equal("SanFrancisco", () => selectedElementText.Text);
        Browser.False(() => ElementHasAttribute(selectElement.WrappedElement, "class"));

        selectElement.SelectByIndex(2);
        selectElement.SelectByIndex(3);

        // Value binding continues to work without an edit context and the class attribute is unchanged
        Browser.Equal("SanFrancisco, London, Madrid", () => selectedElementText.Text);
        Browser.False(() => ElementHasAttribute(selectElement.WrappedElement, "class"));
    }

    [Fact]
    public void InputRadioGroupWorksWithoutEditContext()
    {
        var appElement = Browser.MountTestComponent<InputsWithoutEditForm>();
        var selectedInputText = appElement.FindElement(By.Id("selected-airline-text"));

        // The bound value is expected and no inputs have a class attribute
        Browser.True(() => FindRadioInputs().All(input => !ElementHasAttribute(input, "class")));
        Browser.True(() => FindRadioInputs().First(input => input.GetAttribute("value") == "Unknown").Selected);
        Browser.Equal("Unknown", () => selectedInputText.Text);

        FindRadioInputs().First().Click();

        // Value binding continues to work without an edit context and class attributes are unchanged
        Browser.True(() => FindRadioInputs().All(input => !ElementHasAttribute(input, "class")));
        Browser.True(() => FindRadioInputs().First(input => input.GetAttribute("value") == "BestAirline").Selected);
        Browser.Equal("BestAirline", () => selectedInputText.Text);

        IReadOnlyCollection<IWebElement> FindRadioInputs()
            => appElement.FindElement(By.ClassName("airlines")).FindElements(By.TagName("input"));
    }

    private Func<string[]> CreateValidationMessagesAccessor(IWebElement appElement)
    {
        return () => appElement.FindElements(By.ClassName("validation-message"))
            .Select(x => x.Text)
            .OrderBy(x => x)
            .ToArray();
    }

    private void EnsureAttributeValue(IWebElement element, string attributeName, string value)
    {
        Browser.Equal(value, () => element.GetAttribute(attributeName));
    }

    private void EnsureAttributeNotRendered(IWebElement element, string attributeName)
    {
        Browser.True(() => element.GetAttribute(attributeName) == null);
    }

    private bool ElementHasAttribute(IWebElement webElement, string attribute)
    {
        var jsExecutor = (IJavaScriptExecutor)Browser;
        return (bool)jsExecutor.ExecuteScript($"return arguments[0].attributes['{attribute}'] !== undefined;", webElement);
    }
}
