// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class EventCallbackTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public EventCallbackTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        // On WebAssembly, page reloads are expensive so skip if possible
        Navigate(ServerPathBase);
        Browser.MountTestComponent<BasicTestApp.EventCallbackTest.EventCallbackCases>();
    }

    [Theory]
    [InlineData("capturing_lambda")]
    [InlineData("unbound_lambda")]
    [InlineData("unbound_lambda_nested")]
    [InlineData("unbound_lambda_strongly_typed")]
    [InlineData("unbound_lambda_child_content")]
    [InlineData("unbound_lambda_bind_to_component")]
    public void EventCallback_RerendersOuterComponent(string @case)
    {
        var target = Browser.Exists(By.CssSelector($"#{@case} button"));
        var count = Browser.Exists(By.Id("render_count"));
        Browser.Equal("Render Count: 1", () => count.Text);
        target.Click();
        Browser.Equal("Render Count: 2", () => count.Text);
    }
}
