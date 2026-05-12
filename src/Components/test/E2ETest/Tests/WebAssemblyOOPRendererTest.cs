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

public class WebAssemblyOOPRendererTest(
    BrowserFixture browserFixture,
    OOPRendererServerFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<OOPRendererServerFixture<RazorComponentEndpointsStartup<GlobalInteractivityApp>>>(browserFixture, serverFixture, output)
{
    [ConditionalFact]
    [SkipMultithreadedMono]
    public void OOPRendererIsActiveAndComponentIsInteractive()
    {
        Navigate($"{ServerPathBase}/oop-renderer-interactivity");

        // Wait for WebAssembly to be interactive
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Verify OOP renderer env var is active
        Browser.Equal("true", () => Browser.Exists(By.Id("oop-renderer-active")).Text);
    }

    [ConditionalFact]
    [SkipMultithreadedMono]
    public void OOPRendererHandlesClickEvents()
    {
        Navigate($"{ServerPathBase}/oop-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Click counter button multiple times
        var button = Browser.Exists(By.Id("increment-button"));
        button.Click();
        button.Click();
        button.Click();

        Browser.Equal("3", () => Browser.Exists(By.Id("counter-value")).Text);
    }

    [ConditionalFact]
    [SkipMultithreadedMono]
    public void OOPRendererHandlesTextInputBinding()
    {
        Navigate($"{ServerPathBase}/oop-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        const string text = "Hello OOP Renderer";

        var input = Browser.Exists(By.Id("text-input"));
        input.SendKeys(text);

        Browser.Equal(text, () => Browser.Exists(By.Id("text-output")).Text);
    }

    [ConditionalFact]
    [SkipMultithreadedMono]
    public void OOPRendererHandlesJSInterop()
    {
        Navigate($"{ServerPathBase}/oop-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        Browser.Exists(By.Id("jsinterop-button")).Click();

        Browser.Equal("Hello from JS", () => Browser.Exists(By.Id("jsinterop-result")).Text);
    }

    [ConditionalFact]
    [SkipMultithreadedMono]
    public void OOPRendererHandlesConditionalRendering()
    {
        Navigate($"{ServerPathBase}/oop-renderer-interactivity");
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

    [ConditionalFact]
    [SkipMultithreadedMono]
    public void OOPRendererHandlesListRendering()
    {
        Navigate($"{ServerPathBase}/oop-renderer-interactivity");
        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);

        // Initially 2 items
        var list = Browser.Exists(By.Id("item-list"));
        Browser.Equal(2, () => list.FindElements(By.TagName("li")).Count);

        // Add an item
        Browser.Exists(By.Id("add-item-button")).Click();
        Browser.Equal(3, () => list.FindElements(By.TagName("li")).Count);
    }
}
