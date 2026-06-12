// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class TextareaMethodInteractiveTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public TextareaMethodInteractiveTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"{ServerPathBase}/textarea-method-interactive");
    }

    [Fact]
    public void TextareaAndTextbox_BehaviorAcrossMethodSwitching()
    {
        // Method 1: initial values shown in both textarea and textbox
        Browser.Click(By.Id("method1-link"));
        Browser.Equal("Initial value for method 1", () => Browser.Exists(By.Id("textArea")).GetDomProperty("value"));
        Browser.Equal("Initial value for method 1", () => Browser.Exists(By.Id("textBox")).GetDomProperty("value"));

        // Switch to Method 2: both controls update to method 2 values
        Browser.Click(By.Id("method2-link"));
        Browser.Equal("Method 2 initial value.", () => Browser.Exists(By.Id("textArea")).GetDomProperty("value"));
        Browser.Equal("Method 2 initial value.", () => Browser.Exists(By.Id("textBox")).GetDomProperty("value"));

        // Switch back to Method 1 and edit the textarea
        Browser.Click(By.Id("method1-link"));
        var textArea = Browser.Exists(By.Id("textArea"));
        textArea.Clear();
        textArea.SendKeys("Updated method 1 value");
        textArea.SendKeys("\t"); // trigger onchange
        Browser.Equal("Updated method 1 value", () => Browser.Exists(By.Id("textArea")).GetDomProperty("value"));

        // Switch to Method 2: its value is unaffected by method 1 edit
        Browser.Click(By.Id("method2-link"));
        Browser.Equal("Method 2 initial value.", () => Browser.Exists(By.Id("textArea")).GetDomProperty("value"));
        Browser.Equal("Method 2 initial value.", () => Browser.Exists(By.Id("textBox")).GetDomProperty("value"));

        // Switch back to Method 1: edited value is preserved
        Browser.Click(By.Id("method1-link"));
        Browser.Equal("Updated method 1 value", () => Browser.Exists(By.Id("textArea")).GetDomProperty("value"));
        Browser.Equal("Updated method 1 value", () => Browser.Exists(By.Id("textBox")).GetDomProperty("value"));
    }
}
