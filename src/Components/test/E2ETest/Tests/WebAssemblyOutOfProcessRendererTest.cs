// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class WebAssemblyOutOfProcessRendererTest(
    BrowserFixture browserFixture,
    OutOfProcessRendererServerFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<OutOfProcessRendererServerFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>>>(browserFixture, serverFixture, output)
{
    [Fact]
    public void OutOfProcessRendererIsActiveAndComponentIsInteractive()
    {
        Navigate($"{ServerPathBase}/out-of-process-renderer-interactivity");

        // Wait for WebAssembly to be interactive
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Verify OOP renderer env var is active
        Browser.Equal("true", () => Browser.Exists(By.Id("out-of-process-renderer-active")).Text);
    }

    [Fact]
    public void OutOfProcessRendererHandlesClickEvents()
    {
        Navigate($"{ServerPathBase}/out-of-process-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Click counter button multiple times
        var button = Browser.Exists(By.Id("increment-button"));
        button.Click();
        button.Click();
        button.Click();

        Browser.Equal("3", () => Browser.Exists(By.Id("counter-value")).Text);
    }

    [Fact]
    public void OutOfProcessRendererHandlesTextInputBinding()
    {
        Navigate($"{ServerPathBase}/out-of-process-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        const string text = "Hello OOP Renderer";

        var input = Browser.Exists(By.Id("text-input"));
        input.SendKeys(text);

        Browser.Equal(text, () => Browser.Exists(By.Id("text-output")).Text);
    }

    [Fact]
    public void OutOfProcessRendererHandlesJSInterop()
    {
        Navigate($"{ServerPathBase}/out-of-process-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        Browser.Exists(By.Id("jsinterop-button")).Click();

        Browser.Equal("Hello from JS", () => Browser.Exists(By.Id("jsinterop-result")).Text);
    }

    [Fact]
    public void OutOfProcessRendererHandlesConditionalRendering()
    {
        Navigate($"{ServerPathBase}/out-of-process-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Content should be visible initially
        Browser.Exists(By.Id("toggle-content"));

        // Toggle off
        Browser.Exists(By.Id("toggle-button")).Click();
        Browser.DoesNotExist(By.Id("toggle-content"));

        // Toggle back on
        Browser.Exists(By.Id("toggle-button")).Click();
        Browser.Exists(By.Id("toggle-content"));
    }

    [Fact]
    public void OutOfProcessRendererHandlesListRendering()
    {
        Navigate($"{ServerPathBase}/out-of-process-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Initially 2 items
        var list = Browser.Exists(By.Id("item-list"));
        Browser.Equal(2, () => list.FindElements(By.TagName("li")).Count);

        // Add an item
        Browser.Exists(By.Id("add-item-button")).Click();
        Browser.Equal(3, () => list.FindElements(By.TagName("li")).Count);
    }
}
