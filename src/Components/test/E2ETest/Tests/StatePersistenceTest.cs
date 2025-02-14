// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

// These tests are for Blazor Web implementation
// For Blazor Server and Webassembly, check SaveStateTest.cs
public class StatePersistenceTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    static int _nextStreamingIdContext;

    public StatePersistenceTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    // Separate contexts to ensure that caches and other state don't interfere across tests.
    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext + _nextStreamingIdContext++);

    // Validates that we can use persisted state across server, webassembly, and auto modes, with and without
    // streaming rendering.
    // For streaming rendering, we validate that the state is captured and restored after streaming completes.
    // For enhanced navigation we validate that the state is captured at the time components are rendered for
    // the first time on the page.
    // For auto mode, we validate that the state is captured and restored for both server and wasm runtimes.
    // In each case, we validate that the state is available until the initial set of components first render reaches quiescence. Similar to how it works for Server and WebAssembly.
    // For server we validate that the state is provided every time a circuit is initialized.
    [Theory]
    [InlineData(true, typeof(InteractiveServerRenderMode), (string)null)]
    [InlineData(true, typeof(InteractiveServerRenderMode), "ServerStreaming")]
    [InlineData(true, typeof(InteractiveWebAssemblyRenderMode), (string)null)]
    [InlineData(true, typeof(InteractiveWebAssemblyRenderMode), "WebAssemblyStreaming")]
    [InlineData(true, typeof(InteractiveAutoRenderMode), (string)null)]
    [InlineData(true, typeof(InteractiveAutoRenderMode), "AutoStreaming")]
    [InlineData(false, typeof(InteractiveServerRenderMode), (string)null)]
    [InlineData(false, typeof(InteractiveServerRenderMode), "ServerStreaming")]
    [InlineData(false, typeof(InteractiveWebAssemblyRenderMode), (string)null)]
    [InlineData(false, typeof(InteractiveWebAssemblyRenderMode), "WebAssemblyStreaming")]
    [InlineData(false, typeof(InteractiveAutoRenderMode), (string)null)]
    [InlineData(false, typeof(InteractiveAutoRenderMode), "AutoStreaming")]
    public void CanRenderComponentWithPersistedState(bool suppressEnhancedNavigation, Type renderMode, string streaming)
    {
        var mode = renderMode switch
        {
            var t when t == typeof(InteractiveServerRenderMode) => "server",
            var t when t == typeof(InteractiveWebAssemblyRenderMode) => "wasm",
            var t when t == typeof(InteractiveAutoRenderMode) => "auto",
            _ => throw new ArgumentException($"Unknown render mode: {renderMode.Name}")
        };

        if (!suppressEnhancedNavigation)
        {
            // Navigate to a page without components first to make sure that we exercise rendering components
            // with enhanced navigation on.
            if (streaming == null)
            {
                Navigate($"subdir/persistent-state/page-no-components?render-mode={mode}&suppress-autostart");
            }
            else
            {
                Navigate($"subdir/persistent-state/page-no-components?render-mode={mode}&streaming-id={streaming}&suppress-autostart");
            }
            if (mode == "auto")
            {
                BlockWebAssemblyResourceLoad();
            }
            Browser.Click(By.Id("call-blazor-start"));
            Browser.Click(By.Id("page-with-components-link"));
        }
        else
        {
            EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, true);
            if (mode == "auto")
            {
                BlockWebAssemblyResourceLoad();
            }
        }

        if (mode != "auto")
        {
            RenderComponentsWithPersistentStateAndValidate(suppressEnhancedNavigation, mode, renderMode, streaming);
        }
        else
        {
            // For auto mode, validate that the state is persisted for both runtimes and is able
            // to be loaded on server and wasm.
            RenderComponentsWithPersistentStateAndValidate(suppressEnhancedNavigation, mode, renderMode, streaming, interactiveRuntime: "server");

            UnblockWebAssemblyResourceLoad();
            Browser.Navigate().Refresh();

            RenderComponentsWithPersistentStateAndValidate(suppressEnhancedNavigation, mode, renderMode, streaming, interactiveRuntime: "wasm");
        }
    }

    [Theory]
    [InlineData((string)null)]
    [InlineData("ServerStreaming")]
    public async Task StateIsProvidedEveryTimeACircuitGetsCreated(string streaming)
    {
        var mode = "server";
        if (streaming == null)
        {
            Navigate($"subdir/persistent-state/page-no-components?render-mode={mode}");
        }
        else
        {
            Navigate($"subdir/persistent-state/page-no-components?render-mode={mode}&streaming-id={streaming}");
        }
        Browser.Click(By.Id("page-with-components-link"));

        RenderComponentsWithPersistentStateAndValidate(suppressEnhancedNavigation: false, mode, typeof(InteractiveServerRenderMode), streaming);
        Browser.Click(By.Id("page-no-components-link"));
        // Ensure that the circuit is gone.
        await Task.Delay(1000);
        Browser.Click(By.Id("page-with-components-link-and-state"));
        RenderComponentsWithPersistentStateAndValidate(suppressEnhancedNavigation: false, mode, typeof(InteractiveServerRenderMode), streaming, stateValue: "other");
    }

    private void BlockWebAssemblyResourceLoad()
    {
        // Clear local storage so that the resource hash is not found
        ((IJavaScriptExecutor)Browser).ExecuteScript("localStorage.clear()");

        ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('block-load-boot-resource', 'true')");

        // Clear caches so that we can block the resource load
        ((IJavaScriptExecutor)Browser).ExecuteScript("caches.keys().then(keys => keys.forEach(key => caches.delete(key)))");
    }

    private void UnblockWebAssemblyResourceLoad()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.unblockLoadBootResource()");
        Browser.Exists(By.Id("unblocked-wasm"));
    }

    private void RenderComponentsWithPersistentStateAndValidate(
        bool suppressEnhancedNavigation,
        string mode,
        Type renderMode,
        string streaming,
        string interactiveRuntime = null,
        string stateValue = "restored")
    {
        // No need to navigate if we are using enhanced navigation, the tests will have already navigated to the page via a link.
        if (suppressEnhancedNavigation)
        {
            // In this case we suppress auto start to check some server side state before we boot Blazor.
            if (streaming == null)
            {
                Navigate($"subdir/persistent-state/page-with-components?render-mode={mode}&suppress-autostart");
            }
            else
            {
                Navigate($"subdir/persistent-state/page-with-components?render-mode={mode}&streaming-id={streaming}&suppress-autostart");
            }

            AssertPageState(
                mode: mode,
                renderMode: renderMode.Name,
                interactive: false,
                stateFound: true,
                stateValue: stateValue,
                streamingId: streaming,
                streamingCompleted: false,
                interactiveRuntime: interactiveRuntime);

            Browser.Click(By.Id("call-blazor-start"));
        }

        AssertPageState(
            mode: mode,
            renderMode: renderMode.Name,
            interactive: streaming == null,
            stateFound: true,
            stateValue: stateValue,
            streamingId: streaming,
            streamingCompleted: false,
            interactiveRuntime: interactiveRuntime);

        if (streaming == null)
        {
            return;
        }

        Browser.Click(By.Id("end-streaming"));

        AssertPageState(
            mode: mode,
            renderMode: renderMode.Name,
            interactive: true,
            stateFound: true,
            stateValue: stateValue,
            streamingId: streaming,
            streamingCompleted: true,
            interactiveRuntime: interactiveRuntime);
    }

    private void AssertPageState(
        string mode,
        string renderMode,
        bool interactive,
        bool stateFound,
        string stateValue,
        string streamingId = null,
        bool streamingCompleted = false,
        string interactiveRuntime = null)
    {
        Browser.Equal($"Render mode: {renderMode}", () => Browser.FindElement(By.Id("render-mode")).Text);
        Browser.Equal($"Streaming id:{streamingId}", () => Browser.FindElement(By.Id("streaming-id")).Text);
        Browser.Equal($"Interactive: {interactive}", () => Browser.FindElement(By.Id("interactive")).Text);
        if (streamingId == null || streamingCompleted)
        {
            interactiveRuntime = !interactive ? "none" : mode == "server" || mode == "wasm" ? mode : (interactiveRuntime ?? throw new InvalidOperationException("Specify interactiveRuntime for auto mode"));

            Browser.Equal($"Interactive runtime: {interactiveRuntime}", () => Browser.FindElement(By.Id("interactive-runtime")).Text);
            Browser.Equal($"State found:{stateFound}", () => Browser.FindElement(By.Id("state-found")).Text);
            Browser.Equal($"State value:{stateValue}", () => Browser.FindElement(By.Id("state-value")).Text);
        }
        else
        {
            Browser.Equal("Streaming: True", () => Browser.FindElement(By.Id("streaming")).Text);
        }
    }
}
