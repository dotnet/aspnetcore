// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Tests;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerJSRootComponentsTest : JSRootComponentsTest
{
    public ServerJSRootComponentsTest(BrowserFixture browserFixture, ToggleExecutionModeServerFixture<Program> serverFixture, ITestOutputHelper output)
        : base(browserFixture, serverFixture.WithServerExecution(), output)
    {
    }

    [Fact]
    public void CannotExceedTheConfiguredNumberOfJSComponents()
    {
        // The test app allows 5 JS components
        var addButton = app.FindElement(By.Id("add-root-component"));
        addButton.Click();
        addButton.Click();
        addButton.Click();
        addButton.Click();
        addButton.Click();

        // If we dispose one, that frees up one slot
        app.FindElement(By.Id("remove-root-component")).Click();

        // ... so we can add a new one, that will work
        addButton.Click();
        var dynamicRootContainer = Browser.FindElement(By.Id("root-container-6"));
        Browser.Equal("0", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        Browser.Equal("2", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

        // If we don't dispose one, we can't add another
        addButton.Click();

        // Check the UI did update by showing that our previous component still works
        dynamicRootContainer.FindElement(By.ClassName("increment")).Click();
        Browser.Equal("3", () => dynamicRootContainer.FindElement(By.ClassName("click-count")).Text);

        // Here's where we check that our most recent attempt to add another didn't do it
        Browser.Empty(() => Browser.FindElement(By.Id("root-container-7")).FindElements(By.CssSelector("*")));
    }
}
