// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using BasicTestApp.FormsTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

// These tests only run on WebAssembly, not Server. They are flaky on Server (see #35018, #34884) and our numerous
// attempts to fix this which never gave us better than about 99% reliability. The underlying issue seems to be something
// to do with the timing of keyboard input and the asynchrony of Server. This doesn't appear to affect real use on Server.
// Since it's better at least to have test coverage on WebAssembly, and since this is very specific to one particular type
// of input component, we'll just cover it on that platform.

public class FormsInputDateTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public FormsInputDateTest(
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
    public void InputDateInteractsWithEditContext_NonNullableDateTime()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var renewalDateInput = appElement.FindElement(By.ClassName("renewal-date")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // InputDate emits unmatched attributes
        Browser.Equal("Enter the date", () => renewalDateInput.GetDomAttribute("placeholder"));

        // Validates on edit
        Browser.Equal("valid", () => renewalDateInput.GetDomAttribute("class"));
        renewalDateInput.SendKeys($"{Keys.Backspace}\t{Keys.Backspace}\t{Keys.Backspace}\t");
        renewalDateInput.SendKeys("01/01/2000\t");
        Browser.Equal("modified valid", () => renewalDateInput.GetDomAttribute("class"));

        // Can become invalid
        renewalDateInput.SendKeys("11-11-11111\t");
        Browser.Equal("modified invalid", () => renewalDateInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The RenewalDate field must be a date." }, messagesAccessor);

        // Empty is invalid, because it's not nullable
        renewalDateInput.SendKeys($"{Keys.Backspace}\t{Keys.Backspace}\t{Keys.Backspace}\t");
        Browser.Equal("modified invalid", () => renewalDateInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The RenewalDate field must be a date." }, messagesAccessor);

        // Can become valid
        renewalDateInput.SendKeys("01/01/01\t");
        Browser.Equal("modified valid", () => renewalDateInput.GetDomAttribute("class"));
        Browser.Empty(messagesAccessor);
    }

    [Fact]
    public void InputDateInteractsWithEditContext_NullableDateTimeOffset()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var expiryDateInput = appElement.FindElement(By.ClassName("expiry-date")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // Validates on edit
        Browser.Equal("valid", () => expiryDateInput.GetDomAttribute("class"));
        expiryDateInput.SendKeys("01-01-2000\t");
        Browser.Equal("modified valid", () => expiryDateInput.GetDomAttribute("class"));

        // Can become invalid
        expiryDateInput.SendKeys("11-11-11111\t");
        Browser.Equal("modified invalid", () => expiryDateInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The OptionalExpiryDate field must be a date." }, messagesAccessor);

        // Empty is valid, because it's nullable
        expiryDateInput.SendKeys($"{Keys.Backspace}\t{Keys.Backspace}\t{Keys.Backspace}\t");
        Browser.Equal("modified valid", () => expiryDateInput.GetDomAttribute("class"));
        Browser.Empty(messagesAccessor);
    }

    [Fact]
    public void InputDateInteractsWithEditContext_TimeInput()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);
        var departureTimeInput = appElement.FindElement(By.ClassName("departure-time")).FindElement(By.Id("time-input"));
        var includeSecondsCheckbox = appElement.FindElement(By.ClassName("departure-time")).FindElement(By.Id("time-seconds-checkbox"));

        // Ensure we're not using a custom step
        if (includeSecondsCheckbox.Selected)
        {
            includeSecondsCheckbox.Click();
        }

        // Validates on edit
        Browser.Equal("valid", () => departureTimeInput.GetDomAttribute("class"));
        departureTimeInput.SendKeys("06:43\t");
        Browser.Equal("modified valid", () => departureTimeInput.GetDomAttribute("class"));

        // Can become invalid
        // Stricly speaking the following is equivalent to the empty state, because that's how incomplete input is represented
        // We don't know of any way to produce a different (non-empty-equivalent) state using UI gestures, so there's nothing else to test
        departureTimeInput.SendKeys($"20{Keys.Backspace}\t");
        Browser.Equal("modified invalid", () => departureTimeInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The DepartureTime field must be a time." }, messagesAccessor);
    }

    [Fact(Skip = "This functionality doesn't work on Edge/Chrome - tracked as https://github.com/dotnet/aspnetcore/issues/38471")]
    public void InputDateInteractsWithEditContext_TimeInput_Step()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);
        var departureTimeInput = appElement.FindElement(By.ClassName("departure-time")).FindElement(By.Id("time-input"));
        var includeSecondsCheckbox = appElement.FindElement(By.ClassName("departure-time")).FindElement(By.Id("time-seconds-checkbox"));

        // Ensure we're using a custom step
        if (!includeSecondsCheckbox.Selected)
        {
            includeSecondsCheckbox.Click();
        }

        // Input works with seconds value of zero and has the expected final value
        Browser.Equal("valid", () => departureTimeInput.GetDomAttribute("class"));
        departureTimeInput.SendKeys("111111");
        Browser.Equal("modified valid", () => departureTimeInput.GetDomAttribute("class"));
        Browser.Equal("11:11:11", () => departureTimeInput.GetDomProperty("value"));

        // Input works with non-zero seconds value
        // Move to the beginning of the input and put the new time
        departureTimeInput.SendKeys(string.Concat(Enumerable.Repeat(Keys.ArrowLeft, 3)) + "101010");
        Browser.Equal("modified valid", () => departureTimeInput.GetDomAttribute("class"));
        Browser.Equal("10:10:10", () => departureTimeInput.GetDomProperty("value"));
    }

    [Fact]
    public void InputDateInteractsWithEditContext_MonthInput()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var visitMonthInput = appElement.FindElement(By.ClassName("visit-month")).FindElement(By.TagName("input"));
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);

        // Validates on edit
        Browser.Equal("valid", () => visitMonthInput.GetDomAttribute("class"));
        visitMonthInput.SendKeys($"03{Keys.ArrowRight}2005\t");
        Browser.Equal("modified valid", () => visitMonthInput.GetDomAttribute("class"));

        // Empty is invalid because it's not nullable
        visitMonthInput.Clear();
        Browser.Equal("modified invalid", () => visitMonthInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The VisitMonth field must be a year and month." }, messagesAccessor);

        // Invalid year (11111)
        visitMonthInput.SendKeys($"11{Keys.ArrowRight}11111\t");
        Browser.Equal("modified invalid", () => visitMonthInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The VisitMonth field must be a year and month." }, messagesAccessor);

        // Can become valid again
        visitMonthInput.Clear();
        visitMonthInput.SendKeys($"11{Keys.ArrowRight}1111\t");
        Browser.Equal("modified valid", () => visitMonthInput.GetDomAttribute("class"));
        Browser.Empty(messagesAccessor);
    }

    [Fact]
    public void InputDateInteractsWithEditContext_DateTimeLocalInput()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);
        var appointmentInput = appElement.FindElement(By.ClassName("appointment-date-time")).FindElement(By.Id("datetime-local-input"));
        var includeSecondsCheckbox = appElement.FindElement(By.ClassName("appointment-date-time")).FindElement(By.Id("datetime-local-seconds-checkbox"));

        // Ensure we're not using a custom step
        if (includeSecondsCheckbox.Selected)
        {
            includeSecondsCheckbox.Click();
        }

        // Validates on edit and has the expected value
        Browser.Equal("valid", () => appointmentInput.GetDomAttribute("class"));
        appointmentInput.SendKeys($"01011970{Keys.ArrowRight}05421");
        Browser.Equal("modified valid", () => appointmentInput.GetDomAttribute("class"));

        // Empty is invalid because it's not nullable
        appointmentInput.Clear();
        Browser.Equal("modified invalid", () => appointmentInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The AppointmentDateAndTime field must be a date and time." }, messagesAccessor);

        // Invalid year (11111)
        appointmentInput.SendKeys($"111111111{Keys.ArrowRight}11111");
        Browser.Equal("modified invalid", () => appointmentInput.GetDomAttribute("class"));
        Browser.Equal(new[] { "The AppointmentDateAndTime field must be a date and time." }, messagesAccessor);

        // Can become valid again
        appointmentInput.Clear();
        appointmentInput.SendKeys($"11111111{Keys.ArrowRight}11111");
        Browser.Equal("modified valid", () => appointmentInput.GetDomAttribute("class"));
        Browser.Empty(messagesAccessor);
    }

    [Fact]
    public void InputDateInteractsWithEditContext_DateTimeLocalInput_Step()
    {
        var appElement = Browser.MountTestComponent<TypicalValidationComponent>();
        var messagesAccessor = CreateValidationMessagesAccessor(appElement);
        var appointmentInput = appElement.FindElement(By.ClassName("appointment-date-time")).FindElement(By.Id("datetime-local-input"));
        var includeSecondsCheckbox = appElement.FindElement(By.ClassName("appointment-date-time")).FindElement(By.Id("datetime-local-seconds-checkbox"));

        // Ensure we're using a custom step
        if (!includeSecondsCheckbox.Selected)
        {
            includeSecondsCheckbox.Click();
        }

        // Input works with seconds value of zero (as in, starting from a zero value, which is the default) and has the expected final value
        Browser.Equal("valid", () => appointmentInput.GetDomAttribute("class"));
        appointmentInput.SendKeys($"11111970{Keys.ArrowRight}114216");
        Browser.Equal("modified valid", () => appointmentInput.GetDomAttribute("class"));
        Browser.Equal("1970-11-11T11:42:16", () => appointmentInput.GetDomProperty("value"));

        // Input works when starting with a non-zero seconds value
        // Move to the beginning of the input and put the new value
        appointmentInput.SendKeys(string.Concat(Enumerable.Repeat(Keys.ArrowLeft, 6)) + $"10101970{Keys.ArrowRight}105321");
        Browser.Equal("modified valid", () => appointmentInput.GetDomAttribute("class"));
        Browser.Equal("1970-10-10T10:53:21", () => appointmentInput.GetDomProperty("value"));
    }

    private Func<string[]> CreateValidationMessagesAccessor(IWebElement appElement)
    {
        return () => appElement.FindElements(By.ClassName("validation-message"))
            .Select(x => x.Text)
            .OrderBy(x => x)
            .ToArray();
    }
}
