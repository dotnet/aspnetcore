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

    protected override void InitializeAsyncCore()
    {
        Navigate("subdir/forms/add-validation-form");
        Browser.Exists(By.Id("is-interactive"));
    }

    [Fact]
    public void FormWithNestedValidation_Works()
    {
        Browser.Exists(By.Id("submit-form")).Click();

        // Validation summary
        var messages = Browser.FindElements(By.CssSelector(".validation-errors > .validation-message"))
            .Select(element => element.Text)
            .ToList();

        var expected = new[] {"Order Name is required.",
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
}
