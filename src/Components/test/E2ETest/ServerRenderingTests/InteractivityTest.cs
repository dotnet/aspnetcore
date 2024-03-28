// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

[CollectionDefinition(nameof(InteractivityTest), DisableParallelization = true)]
public class InteractivityTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public InteractivityTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void CanRenderInteractiveServerComponent()
    {
        // '2' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?server=2");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);

        Browser.Click(By.Id("increment-server"));

        Browser.Equal("2", () => Browser.FindElement(By.Id("count-server")).Text);
    }

    [Fact]
    public void CanRenderInteractiveServerComponentFromRazorClassLibrary()
    {
        // '3' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?server-shared=3");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server-shared")).Text);

        Browser.Click(By.Id("increment-server-shared"));

        Browser.Equal("3", () => Browser.FindElement(By.Id("count-server-shared")).Text);
    }

    [Fact]
    public void CanRenderInteractiveWebAssemblyComponentFromRazorClassLibrary()
    {
        // '4' configures the increment amount.
        Navigate($"{ServerPathBase}/interactive?wasm-shared=4");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm-shared")).Text);

        Browser.Click(By.Id("increment-wasm-shared"));

        Browser.Equal("4", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
    }

    [Fact]
    public void CanRenderInteractiveWebAssemblyComponentFromRazorClassLibraryThatIsNotExplicitlyReferenced()
    {
        Navigate($"{ServerPathBase}/not-explicitly-referenced-in-wasm-code");

        // The element with id success is only rendered when webassembly has successfully loaded the component.
        Browser.Exists(By.Id("success"));
    }

    [Fact]
    public void CanRenderInteractiveServerAndWebAssemblyComponentsAtTheSameTime()
    {
        // '3' and '5' configure the increment amounts.
        Navigate($"{ServerPathBase}/interactive?server-shared=3&wasm-shared=5");

        Browser.Equal("0", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server-shared")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm-shared")).Text);

        Browser.Click(By.Id("increment-server-shared"));
        Browser.Click(By.Id("increment-wasm-shared"));

        Browser.Equal("3", () => Browser.FindElement(By.Id("count-server-shared")).Text);
        Browser.Equal("5", () => Browser.FindElement(By.Id("count-wasm-shared")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_Server(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, serverIncrement: 3));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-server"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);

        var countServerElem = Browser.FindElement(By.Id("count-server"));
        Browser.Equal("0", () => countServerElem.Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("3", () => countServerElem.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_WebAssembly(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, webAssemblyIncrement: 4));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-wasm"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);

        var countWasmElem = Browser.FindElement(By.Id("count-wasm"));
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-wasm"));
        Browser.Equal("4", () => countWasmElem.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseCallSiteRenderMode_ServerAndWebAssembly(bool prerender)
    {
        Navigate(InteractiveCallsiteUrl(prerender, serverIncrement: 10, webAssemblyIncrement: 11));
        Browser.Equal("Call-site interactive components", () => Browser.FindElement(By.TagName("h1")).Text);

        if (prerender)
        {
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-server")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id("count-wasm")).Text);
            Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);
        }
        else
        {
            Browser.DoesNotExist(By.Id("count-server"));
            Browser.DoesNotExist(By.Id("count-wasm"));
        }

        Browser.Exists(By.Id("call-blazor-start")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-wasm")).Text);

        var countServerElem = Browser.FindElement(By.Id("count-server"));
        var countWasmElem = Browser.FindElement(By.Id("count-wasm"));
        Browser.Equal("0", () => countServerElem.Text);
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-server"));
        Browser.Equal("10", () => countServerElem.Text);
        Browser.Equal("0", () => countWasmElem.Text);

        Browser.Click(By.Id("increment-wasm"));
        Browser.Equal("11", () => countWasmElem.Text);
        Browser.Equal("10", () => countServerElem.Text);
    }

    private const string AddServerId = "add-server-counter-link";
    private const string AddServerPrerenderedId = "add-server-counter-prerendered-link";
    private const string AddWebAssemblyId = "add-webassembly-counter-link";
    private const string AddWebAssemblyPrerenderedId = "add-webassembly-counter-prerendered-link";
    private const string AddAutoPrerenderedId = "add-auto-counter-prerendered-link";

    public static readonly TheoryData<string[]> AddCounterLinkSequences = new()
    {
        // One component
        new[] { AddServerPrerenderedId },
        new[] { AddWebAssemblyPrerenderedId },

        // Multiple components, mixing all combinations of Server/WebAssembly and prerendered/non-prerendered
        new[] { AddServerPrerenderedId, AddWebAssemblyId, AddWebAssemblyPrerenderedId, AddServerId },
    };

    [Theory]
    [InlineData(AddServerId)]
    [InlineData(AddServerPrerenderedId)]
    [InlineData(AddWebAssemblyId)]
    [InlineData(AddWebAssemblyPrerenderedId)]
    public void DynamicallyAddedSsrComponent_CanBecomeInteractive_AfterEnhancedNavigation(string addCounterLinkId)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(addCounterLinkId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-0")).Text);

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void MultipleDynamicallyAddedSsrComponents_CanBecomeInteractive_AfterEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void DynamicallyAddedSsrComponents_CanBecomeInteractive_AfterStreamingRenderingCompletes(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));

            // To verify that components use the most up-to-date parameters when they become interactive, we
            // perform SSR updates with new parameter values.
            // The first counter will have an increment amount of 1, the second 2, etc.
            for (var j = 0; j < i; j++)
            {
                Browser.Click(By.Id($"update-counter-link-{i}"));
            }

            if (addCounterLinkIds[i].Contains("prerendered"))
            {
                Browser.Equal("False", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
                Browser.Click(By.Id($"increment-{i}"));
                Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text);
            }
            else
            {
                // Non-prerendered components won't produce any output until they become interactive.
                // We verify this by ensuring that the "action links" exist on the page,
                // but the interactive component does not.
                Browser.Exists(By.Id($"remove-counter-link-{i}"));
                Browser.DoesNotExist(By.Id($"is-interactive-{i}"));
            }
        }

        Browser.Click(By.Id("stop-streaming-link"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal($"{i + 1}", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors(entry => !entry.Message.Contains("Initializer"));
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void DynamicallyAddedSsrComponents_CanBecomeInteractive_AfterInitialPageLoadCompletes(string[] addCounterLinkIds)
    {
        // Navigate directly to the page with streaming enabled so that we can defer the page load.
        Navigate($"{ServerPathBase}/streaming-interactivity?ShouldStream=True");
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Equal("loading", () => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.document.readyState;"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));

            // To verify that components use the most up-to-date parameters when they become interactive, we
            // perform SSR updates with new parameter values.
            // The first counter will have an increment amount of 1, the second 2, etc.
            for (var j = 0; j < i; j++)
            {
                Browser.Click(By.Id($"update-counter-link-{i}"));
            }

            if (addCounterLinkIds[i].Contains("prerendered"))
            {
                Browser.Equal("False", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
                Browser.Click(By.Id($"increment-{i}"));
                Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text);
            }
            else
            {
                // Non-prerendered components won't produce any output until they become interactive.
                // We verify this by ensuring that the "action links" exist on the page,
                // but the interactive component does not.
                Browser.Exists(By.Id($"remove-counter-link-{i}"));
                Browser.DoesNotExist(By.Id($"is-interactive-{i}"));
            }
        }

        Browser.Click(By.Id("stop-streaming-link"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal($"{i + 1}", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    private void EnableClassicInitializers(IWebDriver browser, bool skipNavigation)
    {
        if (!skipNavigation)
        {
            browser.Navigate().GoToUrl($"{new Uri(_serverFixture.RootUri, ServerPathBase)}/");
        }
        ((IJavaScriptExecutor)browser).ExecuteScript("sessionStorage.setItem('enable-classic-initializers', 'true')");
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanReceiveSsrParameterUpdates_FromEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("3", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanReceiveSsrParameterUpdates_FromStreamingRenderingUpdate(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // Components don't become interactive during streaming rendering, so we need to
        // add then via enhanced navigation first
        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("3", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        Browser.Click(By.Id("stop-streaming-link"));

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_GetReinitialized_WhenNoKeyIsProvided_AfterReceivingSsrParameterUpdates_FromEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("disable-keys-link"));
        Browser.Exists(By.Id("enable-keys-link"));

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text); // Resets back to 0 because the parameter changed

            // Ensure that interactivity still works, and the correct parameters have been supplied.
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            for (var j = 0; j < addCounterLinkIds.Length; j++)
            {
                if (j == i)
                {
                    continue;
                }

                // Ensure that other components didn't get reset; their parameters did not change.
                Browser.NotEqual("0", () => Browser.FindElement(By.Id($"count-{j}")).Text);
            }
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_GetReinitialized_WhenNoKeyIsProvided_AfterReceivingSsrParameterUpdates_FromStreamingRenderingUpdate(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("disable-keys-link"));
        Browser.Exists(By.Id("enable-keys-link"));

        // Components don't become interactive during streaming rendering, so we need to
        // add then via enhanced navigation first
        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);
        }

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"update-counter-link-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"increment-amount-{i}")).Text);
            Browser.Equal("0", () => Browser.FindElement(By.Id($"count-{i}")).Text); // Resets back to 0 because the parameter changed

            // Ensure that interactivity still works, and the correct parameters have been supplied.
            // Note that while SSR'd components don't become interactive during stream rendering,
            // this streaming update replaced an already-interactive component. To ensure that supplying
            // unchanged parameters behaves the same way as supplying updated parameters, we
            // check that the component is still interactive.
            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("2", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            for (var j = 0; j < addCounterLinkIds.Length; j++)
            {
                if (j == i)
                {
                    continue;
                }

                // Ensure that other components didn't get reset; their parameters did not change.
                Browser.NotEqual("0", () => Browser.FindElement(By.Id($"count-{j}")).Text);
            }
        }

        Browser.Click(By.Id("stop-streaming-link"));

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanGetDisposed_FromEnhancedNavigation(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"remove-counter-link-{i}"));
            Browser.DoesNotExist(By.Id($"remove-counter-link-{i}"));
            AssertBrowserLogContainsMessage($"Counter {i} was disposed");
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void InteractiveRootComponents_CanGetDisposed_FromStreamingRenderingUpdate(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // Components don't become interactive during streaming rendering, so we need to
        // add then via enhanced navigation first
        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id($"remove-counter-link-{i}"));
            Browser.DoesNotExist(By.Id($"remove-counter-link-{i}"));
            AssertBrowserLogContainsMessage($"Counter {i} was disposed");
        }

        Browser.Click(By.Id("stop-streaming-link"));

        AssertBrowserLogDoesNotContainErrors();
    }

    [Theory]
    [MemberData(nameof(AddCounterLinkSequences))]
    public void DynamicallyAddedSsrComponents_CanGetRemoved_BeforeStreamingRenderingCompletes(string[] addCounterLinkIds)
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < addCounterLinkIds.Length; i++)
        {
            Browser.Click(By.Id(addCounterLinkIds[i]));
            Browser.Exists(By.Id($"remove-counter-link-{i}"));
        }

        Browser.Click(By.Id($"remove-counter-link-0"));

        Browser.Click(By.Id("stop-streaming-link"));

        for (var i = 1; i < addCounterLinkIds.Length; i++)
        {
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);
        }

        AssertBrowserLogDoesNotContainErrors();
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_AfterAddingWebAssemblyRootComponent()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_WhenAddingWebAssemblyComponentAfterServerWasPreviouslyUsed()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        UnblockWebAssemblyResourceLoad();

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServerOnFirstLoad_ThenWebAssemblyOnSuccessiveLoads()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        UnblockWebAssemblyResourceLoad();

        // Add a WebAssembly component to ensure the WebAssembly runtime
        // will be cached after we refresh the page.
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id($"remove-counter-link-1"));
        Browser.DoesNotExist(By.Id("is-interactive-1"));

        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServer_IfBootResourceHashChanges()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        UnblockWebAssemblyResourceLoad();
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id($"remove-counter-link-1"));
        Browser.DoesNotExist(By.Id("is-interactive-1"));

        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        ForceWebAssemblyResourceCacheMiss("dummy hash");
        Browser.Navigate().Refresh();

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_WhenAutoAndWebAssemblyComponentsAreAddedAtTheSameTime()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        ForceWebAssemblyResourceCacheMiss();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id("stop-streaming-link"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);
    }

    [Fact]
    public void AutoRenderMode_CanUseBlazorServer_WhenMultipleAutoComponentsAreAddedAtTheSameTime()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id("stop-streaming-link"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);
    }

    [Fact]
    public void AutoRenderMode_CanUseBlazorWebAssembly_WhenMultipleAutoComponentsAreAddedAtTheSameTime()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        ForceWebAssemblyResourceCacheMiss();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // We start by adding a WebAssembly component to ensure the WebAssembly runtime
        // will be cached after we refresh the page.
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("is-interactive-0"));

        Browser.Navigate().Refresh();

        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("SSR", () => Browser.FindElement(By.Id("render-mode-2")).Text);

        Browser.Click(By.Id("stop-streaming-link"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorWebAssembly_WhenBothServerAndWebAssemblyComponentsExist()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServer_WhenOnlyServerComponentsExist_EvenAfterWebAssemblyResourcesLoad()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        ForceWebAssemblyResourceCacheMiss();

        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        // We start by adding a WebAssembly component to ensure the WebAssembly runtime
        // will be cached after we refresh the page.
        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        Browser.DoesNotExist(By.Id("is-interactive-0"));

        Browser.Navigate().Refresh();

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        // Verify that Auto mode will use Blazor Server, even though the WebAssembly runtime is cached
        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-2")).Text);
    }

    [Fact]
    public void AutoRenderMode_UsesBlazorServer_AfterWebAssemblyComponentsNoLongerExist_ButServerComponentsDo()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-2")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-2")).Text);

        // Remove all WebAssembly components
        Browser.Click(By.Id("remove-counter-link-0"));
        Browser.Click(By.Id("remove-counter-link-2"));

        // Verify that Blazor Server gets used
        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-3")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-3")).Text);
    }

    [Fact]
    public void WebAssemblyRenderMode_DownloadsWebAssemblyResourcesInParallel()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity?ClearSiteData=True");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.True(() => GetMaxParallelWebAssemblyResourceDownloadCount() > 1);
    }

    [Fact]
    public void AutoRenderMode_DoesNotDownloadWebAssemblyResourcesInParallel()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity?ClearSiteData=True");
        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddAutoPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-0")).Text);

        Browser.Click(By.Id(AddWebAssemblyPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-1")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-1")).Text);

        Browser.Equal(1, GetMaxParallelWebAssemblyResourceDownloadCount);
    }

    [Fact]
    public void Circuit_ShutsDown_WhenAllBlazorServerComponentsGetRemoved()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-0")).Text);

        Browser.Click(By.Id("remove-counter-link-0"));

        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();
    }

    [Fact]
    public void Circuit_CanShutDownAndReInitializeMultipleTimes()
    {
        const int numCircuits = 5;

        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        for (var i = 0; i < numCircuits; i++)
        {
            Browser.Click(By.Id(AddServerPrerenderedId));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-{i}")).Text);

            Browser.Click(By.Id($"increment-{i}"));
            Browser.Equal("1", () => Browser.FindElement(By.Id($"count-{i}")).Text);

            Browser.Click(By.Id($"remove-counter-link-{i}"));

            AssertBrowserLogContainsMessage("Connection disconnected.");
            AssertBrowserLogDoesNotContainErrors();
            ClearBrowserLogs();
        }
    }

    [Fact]
    public async Task ReconnectionHandler_DoesNotRun_AfterIntentionallyShuttingDownTheCircuit()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive-0")).Text);

        Browser.Click(By.Id("increment-0"));
        Browser.Equal("1", () => Browser.FindElement(By.Id("count-0")).Text);

        Browser.Click(By.Id("remove-counter-link-0"));

        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();
        ClearBrowserLogs();

        // Wait for the reconnection handler to run, if it's going to. Hopefully it doesn't.
        await Task.Delay(5000);

        AssertBrowserLogDoesNotContainMessage("WebSocket connected to");
    }

    [Fact]
    public void DotNetObjectReference_CannotBeUsed_AfterCircuitShutsDown()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-0")).Text);

        // This JS call works via .NET object reference to a counter component.
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");
        Browser.Equal("1", () => Browser.FindElement(By.Id($"count-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");

        AssertBrowserLogContainsMessage("Error: The circuit");
    }

    [Fact]
    public void DotNetObjectReference_CannotBeUsed_AfterCircuitShutsDown_AndANewCircuitIsInitialized()
    {
        Navigate($"{ServerPathBase}/streaming-interactivity");

        Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-0")).Text);

        // This JS call works via .NET object reference to a counter component.
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");
        Browser.Equal("1", () => Browser.FindElement(By.Id($"count-0")).Text);

        Browser.Click(By.Id($"remove-counter-link-0"));
        AssertBrowserLogContainsMessage("Connection disconnected.");
        AssertBrowserLogDoesNotContainErrors();

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");

        AssertBrowserLogContainsMessage("Error: The circuit");
        ClearBrowserLogs();

        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(1)");
        Browser.Equal("1", () => Browser.FindElement(By.Id($"count-1")).Text);

        AssertBrowserLogDoesNotContainErrors();

        ((IJavaScriptExecutor)Browser).ExecuteScript("window.incrementCounter(0)");
        AssertBrowserLogContainsMessage("Error: The circuit");
    }

    [Fact]
    public void CanPersistPrerenderedState_Server()
    {
        Navigate($"{ServerPathBase}/persist-state?server=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_WebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-state?wasm=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_Auto_PersistsOnWebAssembly()
    {
        Navigate($"{ServerPathBase}/persist-state?auto=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_Auto_PersistsOnServer()
    {
        Navigate(ServerPathBase);
        Browser.Equal("Hello", () => Browser.Exists(By.TagName("h1")).Text);
        BlockWebAssemblyResourceLoad();

        Navigate($"{ServerPathBase}/persist-state?auto=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
    }

    [Fact]
    public void CanPersistState_AllRenderModesAtTheSameTime()
    {
        Navigate($"{ServerPathBase}/persist-state?server=true&wasm=true&auto=true");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);
        Browser.Equal("Server", () => Browser.FindElement(By.Id("render-mode-server")).Text);

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-wasm")).Text);

        Browser.Equal("restored", () => Browser.FindElement(By.Id("auto")).Text);
        Browser.Equal("WebAssembly", () => Browser.FindElement(By.Id("render-mode-auto")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_ServerPrerenderedStateAvailableOnlyOnFirstRender()
    {
        Navigate($"{ServerPathBase}/persist-server-state");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("server")).Text);

        Browser.Click(By.Id("destroy-and-recreate"));

        Browser.Equal("not restored", () => Browser.FindElement(By.Id("server")).Text);
    }

    [Fact]
    public void CanPersistPrerenderedState_WebAssemblyPrerenderedStateAvailableOnlyOnFirstRender()
    {
        Navigate($"{ServerPathBase}/persist-wasm-state");

        Browser.Equal("restored", () => Browser.FindElement(By.Id("wasm")).Text);

        Browser.Click(By.Id("destroy-and-recreate"));

        Browser.Equal("not restored", () => Browser.FindElement(By.Id("wasm")).Text);
    }

    [Fact]
    public void NavigationManagerCanRefreshSSRPageWhenServerInteractivityEnabled()
    {
        Navigate($"{ServerPathBase}/forms/form-that-calls-navigation-manager-refresh");

        var guid = Browser.Exists(By.Id("guid")).Text;

        Browser.Exists(By.Id("submit-button")).Click();

        // Checking that the page was refreshed.
        // The redirect request method is GET.
        // Providing a Guid to check that it is not the initial GET request for the page
        Browser.NotEqual(guid, () => Browser.Exists(By.Id("guid")).Text);
        Browser.Equal("GET", () => Browser.Exists(By.Id("method")).Text);
    }

    [Fact]
    public void InteractiveServerRootComponent_CanAccessCircuitContext()
    {
        Navigate($"{ServerPathBase}/interactivity/circuit-context");

        Browser.Equal("True", () => Browser.FindElement(By.Id("has-circuit-context")).Text);
    }

    [Fact]
    public void InteractiveServerRootComponents_CanBecomeInteractive_WithoutInterferingWithOtherCircuits()
    {
        // Start by setting up 2 tabs with interactive server components.
        SetUpPageWithOneInteractiveServerComponent();

        var firstWindow = Browser.CurrentWindowHandle;
        Browser.SwitchTo().NewWindow(WindowType.Tab);
        var secondWindow = Browser.CurrentWindowHandle;

        SetUpPageWithOneInteractiveServerComponent();

        // Start streaming in the second tab.
        Browser.Click(By.Id("start-streaming-link"));
        Browser.Equal("Streaming", () => Browser.FindElement(By.Id("status")).Text);

        // Add an interactive server component while streaming.
        // This will update the existing component, but the new component
        // won't become interactive until streaming ends.
        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("False", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        // Add an interactive server component in the first tab.
        // This component will become interactive immediately because the response
        // that rendered the component will have completed quickly.
        Browser.SwitchTo().Window(firstWindow);
        Browser.Click(By.Id(AddServerPrerenderedId));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        // Stop streaming in the second tab.
        // This will activate the pending component for interactivity.
        // This check verifies that a circuit can activate components from its most
        // recent response, even if that response isn't the most recent between all
        // circuits.
        Browser.SwitchTo().Window(secondWindow);
        Browser.Click(By.Id("stop-streaming-link"));
        Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-1")).Text);

        void SetUpPageWithOneInteractiveServerComponent()
        {
            Navigate($"{ServerPathBase}/streaming-interactivity");

            Browser.Equal("Not streaming", () => Browser.FindElement(By.Id("status")).Text);

            Browser.Click(By.Id(AddServerPrerenderedId));
            Browser.Equal("True", () => Browser.FindElement(By.Id($"is-interactive-0")).Text);
        }
    }

    private void BlockWebAssemblyResourceLoad()
    {
        // Force a WebAssembly resource cache miss so that we can fall back to using server interactivity
        ForceWebAssemblyResourceCacheMiss();

        ((IJavaScriptExecutor)Browser).ExecuteScript("sessionStorage.setItem('block-load-boot-resource', 'true')");

        // Clear caches so that we can block the resource load
        ((IJavaScriptExecutor)Browser).ExecuteScript("caches.keys().then(keys => keys.forEach(key => caches.delete(key)))");
    }

    private void UnblockWebAssemblyResourceLoad()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.unblockLoadBootResource()");
    }

    private void ForceWebAssemblyResourceCacheMiss(string resourceHash = null)
    {
        if (resourceHash is not null)
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript($"localStorage.setItem('blazor-resource-hash:Components.WasmMinimal', '{resourceHash}')");
        }
        else
        {
            // Clear local storage so that the resource hash is not found
            ((IJavaScriptExecutor)Browser).ExecuteScript("localStorage.clear()");
        }
    }

    private long GetMaxParallelWebAssemblyResourceDownloadCount()
    {
        return (long)((IJavaScriptExecutor)Browser).ExecuteScript("return window['__aspnetcore__testing__max__parallel__resource__download__count'] || 0;");
    }

    private string InteractiveCallsiteUrl(bool prerender, int? serverIncrement = default, int? webAssemblyIncrement = default)
    {
        var result = $"{ServerPathBase}/interactive-callsite?suppress-autostart&prerender={prerender}";

        if (serverIncrement.HasValue)
        {
            result += $"&server={serverIncrement}";
        }

        if (webAssemblyIncrement.HasValue)
        {
            result += $"&wasm={webAssemblyIncrement}";
        }

        return result;
    }

    private void AssertBrowserLogContainsMessage(string message)
        => Browser.True(() => DoesBrowserLogContainMessage(message));

    private void AssertBrowserLogDoesNotContainMessage(string message)
        => Browser.False(() => DoesBrowserLogContainMessage(message));

    private bool DoesBrowserLogContainMessage(string message)
    {
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        return entries.Any(entry => entry.Message.Contains(message));
    }

    private void AssertBrowserLogDoesNotContainErrors(Func<LogEntry, bool> filter = null)
    {
        var entries = Browser.Manage().Logs.GetLog(LogType.Browser);
        if (filter != null)
        {
            entries = entries.Where(filter).ToArray().AsReadOnly();
        }
        Assert.DoesNotContain(entries, entry => entry.Level == LogLevel.Severe);
    }

    private void ClearBrowserLogs()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
    }
}
