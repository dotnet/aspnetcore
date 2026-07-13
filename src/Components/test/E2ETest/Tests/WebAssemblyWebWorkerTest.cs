// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class WebAssemblyWebWorkerTest(
    BrowserFixture browserFixture,
    BlazorWasmTestAppFixture<Wasm.WebWorker.Client.Program> serverFixture,
    ITestOutputHelper output)
    : ServerTestBase<BlazorWasmTestAppFixture<Wasm.WebWorker.Client.Program>>(browserFixture, serverFixture, output)
{
    [Fact]
    public void StandaloneWebAssemblyScriptRunsInWebWorkerAndIsInteractive()
    {
        NavigateAndWaitForStandaloneWebWorker();

        Browser.Equal("webassembly", () => Browser.Exists(By.Id("execution-mode")).Text);
        Browser.Equal("true", () => Browser.Exists(By.Id("web-worker-active")).Text);
    }

    [Fact]
    public void StandaloneWebAssemblyScriptHandlesClickEvents()
    {
        NavigateAndWaitForStandaloneWebWorker();

        var button = Browser.Exists(By.Id("increment-button"));
        button.Click();
        button.Click();
        button.Click();

        Browser.Equal("3", () => Browser.Exists(By.Id("counter-value")).Text);
    }

    [Fact]
    public void StandaloneWebAssemblyScriptHandlesTextInputBinding()
    {
        NavigateAndWaitForStandaloneWebWorker();

        const string text = "Hello Web Worker";

        var input = Browser.Exists(By.Id("text-input"));
        input.SendKeys(text);

        Browser.Equal(text, () => Browser.Exists(By.Id("text-output")).Text);
    }

    [Fact]
    public void StandaloneWebAssemblyScriptHandlesJSInterop()
    {
        NavigateAndWaitForStandaloneWebWorker();

        Browser.Exists(By.Id("jsinterop-button")).Click();

        Browser.Equal("Hello from JS", () => Browser.Exists(By.Id("jsinterop-result")).Text);
    }

    [Fact]
    public void StandaloneWebAssemblyScriptHandlesConditionalRendering()
    {
        NavigateAndWaitForStandaloneWebWorker();

        Browser.Exists(By.Id("toggle-content"));

        Browser.Exists(By.Id("toggle-button")).Click();
        Browser.DoesNotExist(By.Id("toggle-content"));

        Browser.Exists(By.Id("toggle-button")).Click();
        Browser.Exists(By.Id("toggle-content"));
    }

    [Fact]
    public void StandaloneWebAssemblyScriptHandlesListRendering()
    {
        NavigateAndWaitForStandaloneWebWorker();

        var list = Browser.Exists(By.Id("item-list"));
        Browser.Equal(2, () => list.FindElements(By.TagName("li")).Count);

        Browser.Exists(By.Id("add-item-button")).Click();
        Browser.Equal(3, () => list.FindElements(By.TagName("li")).Count);
    }

    private void NavigateAndWaitForStandaloneWebWorker()
    {
        Navigate("/");
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window['__aspnetcore__testing__blazor_wasm__started__'] === true;"));
    }
}
