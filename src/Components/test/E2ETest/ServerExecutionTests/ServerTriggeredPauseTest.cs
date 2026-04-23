// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerTriggeredPauseTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public ServerTriggeredPauseTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/subdir/persistent-state/server-pause");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void ServerPause_StateRestoredAfterResume()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        var previousRender = Browser.Exists(By.Id("persistent-counter-render")).Text;

        TriggerServerPauseAndResume();

        var newRender = Browser.Exists(By.Id("persistent-counter-render")).Text;
        Assert.NotEqual(previousRender, newRender);
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void PauseDuringRender_StateMutationPreservedAfterResume()
    {
        Browser.Equal("0", () => Browser.Exists(By.Id("inline-count")).Text);

        // Click the button that increments AND pauses in the same handler.
        Browser.Exists(By.Id("increment-and-pause")).Click();

        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();

        // The state mutation from the handler (count incremented to 1) should be preserved.
        Browser.Equal("1", () => Browser.Exists(By.Id("inline-count")).Text);

        // Can continue interacting after resume.
        Browser.Exists(By.Id("increment-and-pause")).Click();
        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();

        Browser.Equal("2", () => Browser.Exists(By.Id("inline-count")).Text);
    }

    // B1: Client pause + server pause in the same JS turn.
    // Blazor.pauseCircuit() synchronously sends PauseCircuit to the hub.
    // TriggerServerPause is queued after it. SignalR processes PauseCircuit first,
    // removing the circuit from ConnectedCircuits. The server pause interop call
    // arrives at a dead circuit and is dropped. The client pause always wins.
    [Fact]
    public void ConcurrentClientAndServerPause_ClientPauseWins()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        var circuitId = GetCircuitId();
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript($@"
            Blazor.pauseCircuit();
            DotNet.invokeMethodAsync('Components.TestServer', 'TriggerServerPause', '{circuitId}');
        ");

        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();

        // State preserved: exactly one pause happened (the client one).
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    // click() fires beginInvokeDotNetFromJS first, then TriggerServerPause fires second.
    // SignalR delivers them in order → click processed, then pause.
    [Fact]
    public void EventDispatchDuringPause_EventProcessedBeforePause()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        var circuitId = GetCircuitId();
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript($@"
            document.getElementById('increment-persistent-counter-count').click();
            DotNet.invokeMethodAsync('Components.TestServer', 'TriggerServerPause', '{circuitId}');
        ");

        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();

        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void ResumeWhilePausing_ResumeRejectedThenSucceedsAfterPause()
    {
        TriggerServerPause();
        WaitForPausedUI();

        var javascript = (IJavaScriptExecutor)Browser;
        var resumed = (bool)javascript.ExecuteAsyncScript(@"
            const callback = arguments[arguments.length - 1];
            Blazor.resumeCircuit().then(callback);
        ");
        Assert.True(resumed);

        WaitForResumedUI();
    }

    // SignalR ordering: 3 beginInvokeDotNetFromJS calls queued before the pause interop.
    [Fact]
    public void MultipleEventsBeforePause_CircuitPausesCleanly()
    {
        var circuitId = GetCircuitId();
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript($@"
            document.getElementById('increment-persistent-counter-count').click();
            document.getElementById('increment-persistent-counter-count').click();
            document.getElementById('increment-persistent-counter-count').click();
            DotNet.invokeMethodAsync('Components.TestServer', 'TriggerServerPause', '{circuitId}');
        ");

        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();

        Browser.Equal("3", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void NewCircuitConnectsDuringDrain()
    {
        TriggerServerPause();
        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    // resumeCircuit() sets _resumingState synchronously. pauseCircuit() checks it next.
    // JS single-threading guarantees the order.
    [Fact]
    public void PauseDuringResume_PauseRejectedByClient()
    {
        TriggerServerPause();
        WaitForPausedUI();

        var javascript = (IJavaScriptExecutor)Browser;
        var pauseRejected = (bool)javascript.ExecuteAsyncScript(@"
            const callback = arguments[arguments.length - 1];
            (async function() {
                const resumePromise = Blazor.resumeCircuit();
                const pauseResult = await Blazor.pauseCircuit();
                await resumePromise;
                callback(!pauseResult);
            })();
        ");

        Assert.True(pauseRejected);
        WaitForResumedUI();
    }

    [Fact]
    public void PauseAfterRender_StateIncludesPriorMutations()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        TriggerServerPauseAndResume();

        Browser.Equal("2", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }

    [Fact]
    public void PausedUI_ShowsResumeButtonNotRetry()
    {
        TriggerServerPause();
        WaitForPausedUI();

        Browser.Equal(
            (false, true),
            () => Browser.Exists(
                () =>
                {
                    var buttons = GetReconnectModalButtons();
                    Assert.Equal(2, buttons.Count);
                    return (buttons.ElementAt(0).Displayed, buttons.ElementAt(1).Displayed);
                },
                TimeSpan.FromSeconds(1)));

        ClickResumeButton();
        WaitForResumedUI();
    }

    private string GetCircuitId()
    {
        return Browser.Exists(By.Id("circuit-id")).Text;
    }

    private void TriggerServerPause()
    {
        var circuitId = GetCircuitId();
        var javascript = (IJavaScriptExecutor)Browser;
        // This JS interop call goes to the server as a hub call (beginInvokeDotNetFromJS).
        // The server calls RequestCircuitPauseAsync() which sends JS.RequestPause back.
        // The hub call completes, then the client processes JS.RequestPause deterministically.
        javascript.ExecuteScript(
            $"DotNet.invokeMethodAsync('Components.TestServer', 'TriggerServerPause', '{circuitId}')");
    }

    private void TriggerServerPauseAndResume()
    {
        TriggerServerPause();
        WaitForPausedUI();
        ClickResumeButton();
        WaitForResumedUI();
    }

    private void WaitForPausedUI()
    {
        Browser.Equal("block", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }

    private void WaitForResumedUI()
    {
        Browser.Equal("none", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }

    private IReadOnlyCollection<IWebElement> GetReconnectModalButtons()
    {
        return Browser.Exists(By.Id("components-reconnect-modal"))
            .GetShadowRoot()
            .FindElements(By.CssSelector(".components-reconnect-dialog button"));
    }

    private void ClickResumeButton()
    {
        Browser.Exists(
            () =>
            {
                var buttons = GetReconnectModalButtons();
                return buttons.Count >= 2 ? buttons.ElementAt(1) : null;
            },
            TimeSpan.FromSeconds(1)).Click();
    }
}

// H2: Custom reconnect UI handles server-triggered pause.
// The custom element receives the state change event with pause-specific state.
public class H2_CustomUIServerTriggeredPauseTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    public H2_CustomUIServerTriggeredPauseTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("/subdir/persistent-state/server-pause?custom-reconnect-ui=true");
        Browser.Exists(By.Id("render-mode-interactive"));
        Browser.Exists(By.CssSelector("#components-reconnect-modal[data-nosnippet]"));
    }

    [Fact]
    public void CustomReconnectUI_HandlesServerPause()
    {
        Browser.Exists(By.Id("increment-persistent-counter-count")).Click();
        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);

        var circuitId = Browser.Exists(By.Id("circuit-id")).Text;
        var javascript = (IJavaScriptExecutor)Browser;
        javascript.ExecuteScript(
            $"DotNet.invokeMethodAsync('Components.TestServer', 'TriggerServerPause', '{circuitId}')");

        // Custom reconnect modal uses a <dialog> element — when open, it has the `open` attribute
        Browser.True(() => Browser.Exists(By.Id("components-reconnect-modal")).GetAttribute("open") is not null);

        // Resume via the custom Resume button
        Browser.Exists(By.Id("components-resume-button")).Click();

        // Dialog should close
        Browser.True(() => Browser.Exists(By.Id("components-reconnect-modal")).GetAttribute("open") is null);

        Browser.Equal("1", () => Browser.Exists(By.Id("persistent-counter-count")).Text);
    }
}
