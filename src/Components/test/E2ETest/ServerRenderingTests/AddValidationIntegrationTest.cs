// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class AddValidationIntegrationTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public AddValidationIntegrationTest(BrowserFixture browserFixture, BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture, ITestOutputHelper output) : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void FormWithNestedValidation_Works()
    {
        Navigate("subdir/forms/add-validation-form");
        Browser.Exists(By.Id("is-interactive"));

        Browser.Exists(By.Id("submit-form")).Click();

        Browser.Exists(By.Id("is-invalid"));

        // Validation summary
        var messageElements = Browser.FindElements(By.CssSelector(".validation-errors > .validation-message"));

        var messages = messageElements.Select(element => element.Text)
            .ToList();

        var expected = new[]
        {
            "Order Name is required.",
            "Full Name is required.",
            "Email is required.",
            "Street is required.",
            "Zip Code is required.",
            "Product Name is required."
        };

        Assert.Equal(expected, messages);

        // Individual field messages
        var individual = Browser.FindElements(By.CssSelector(".mb-3 > .validation-message"))
            .Select(element => element.Text)
            .ToList();

        Assert.Equal(expected, individual);
    }

    [Fact]
    public void FormWithTypeLevelValidations_Works()
    {
        // This is the new way of initializing test page
        Navigate("subdir/forms/type-level-validation-form");
        Browser.Exists(By.Id("is-interactive"));

        // These have been replaced to use Browser.FindElement directly
        var isCatCheckbox = Browser.FindElement(By.ClassName("cattiness")).FindElement(By.TagName("input"));
        var ageInput = Browser.FindElement(By.ClassName("age")).FindElement(By.TagName("input"));
        var submitButton = Browser.FindElement(By.CssSelector("button[type=submit]"));
        var modelMessagesAccessor = CreateValidationMessagesAccessor(
            Browser.FindElement(By.ClassName("model-errors")),
            "ul.model-summary-custom-class > .validation-message"); // This shows we can override the ul's CSS class
        var allMessagesAccessor = CreateValidationMessagesAccessor(
            Browser.FindElement(By.ClassName("all-errors")));

        //// Cause a property-level validation error
        ageInput.Clear();
        ageInput.SendKeys("-1");
        submitButton.Click();
        Browser.Collection(allMessagesAccessor, x => Assert.Equal("Under-zeros should not be filling out forms", x));
        Browser.Empty(modelMessagesAccessor);

        //// Cause a model-level validation error
        ageInput.Clear();
        ageInput.SendKeys("10");
        submitButton.Click();
        Browser.Collection(allMessagesAccessor, x => Assert.Equal("Sorry, you're not old enough as a non-cat", x));
        Browser.Collection(modelMessagesAccessor, x => Assert.Equal("Sorry, you're not old enough as a non-cat", x));

        //// Become valid
        isCatCheckbox.Click();
        submitButton.Click();
        Browser.Empty(allMessagesAccessor);
        Browser.Empty(modelMessagesAccessor);

        Func<string[]> logEntries = () => Browser.FindElements(By.ClassName("submission-log-entry")).Select(x => x.Text).ToArray();
        Browser.Collection(logEntries, x => Assert.Equal("OnValidSubmit", x));
    }

    private Func<string[]> CreateValidationMessagesAccessor(IWebElement appElement, string messageSelector = ".validation-message")
    {
        return () => appElement.FindElements(By.CssSelector(messageSelector))
            .Select(x => x.Text)
            .OrderBy(x => x)
            .ToArray();
    }
}
