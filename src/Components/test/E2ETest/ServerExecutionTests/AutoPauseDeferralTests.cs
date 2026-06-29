// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Json;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class AutoPauseDeferralTests : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>>>
{
    // Must be short enough to keep tests fast but long enough to be reliably observable.
    private const int PauseDelayMs = 200;

    public AutoPauseDeferralTests(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<Root>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--DisableReconnectionCache", "true", "--AllowLargeHubMessages", "true");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"/subdir/persistent-state/auto-pause-download?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        // Only rendered when the component is interactive — reliable readiness signal.
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    private void NavigateToUploadPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-upload?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    private void NavigateToJSInteropPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-jsinterop?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    private void NavigateToTypingPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-typing?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    // server streams bytes to JS over the circuit, pausing would cause exceptions
    public void DotNetStreamReference_DoesNotPause_WhileStreamInFlight()
        => RunDeferralTest("streamref-button", expectDeferral: true);

    [Fact]
    // browser opens download in new tab, circuit not involved, pause as normal
    public void AnchorTargetBlank_PausesNormally_WhileHttpDownloadInFlight()
        => RunDeferralTest("anchor-blank-link", expectDeferral: false);

    [Fact]
    // browser handles attachment download, circuit not involved, pause as normal
    public void AnchorDownloadAttribute_PausesNormally_WhileHttpDownloadInFlight()
        => RunDeferralTest("anchor-download-link", expectDeferral: false);

    [Fact]
    // full-page navigation that bypasses the circuit
    public void NavigateToForceLoad_DoesNotCrashCircuit()
    {
        var token = ReadToken("navigate-button");
        Browser.Exists(By.Id("navigate-button")).Click();
        WaitForStreamStarted(token);
        ReleaseGate(token);
    }

    [Fact]
    // client streams to server over the circuit, InputFile uses IJSStreamReference
    public void InputFile_DoesNotPause_WhileUploadInFlight()
    {
        NavigateToUploadPage();
        var input = Browser.Exists(By.Id("inputfile-upload"));
        var token = input.GetDomAttribute("data-token")!;
        var tempFile = Path.Combine(Path.GetTempPath(), $"autopause-upload-{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(tempFile, new byte[256 * 1024]);
        try
        {
            RunDeferralTest(token, () => input.SendKeys(tempFile), expectDeferral: true);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* best effort */ }
        }
    }

    [Fact]
    // direct upload via IJSStreamReference
    public void IJSStreamReference_DoesNotPause_WhileUploadInFlight()
    {
        NavigateToUploadPage();
        RunDeferralTest("streamref-upload-button", expectDeferral: true);
    }

    [Fact]
    // pure HTTP POST, circuit not involved, pause as normal
    public void HttpFetchPost_PausesNormally_WhileUploadInFlight()
    {
        NavigateToUploadPage();
        RunDeferralTest("fetch-upload-button", expectDeferral: false);
    }

    [Fact]
    // .NET awaits a JS function via JS.InvokeAsync, non-streamed
    public void JSInterop_DotNetToJS_DoesNotPause_WhileCallInFlight()
    {
        NavigateToJSInteropPage();
        RunDeferralTest("dotnet-to-js-button", expectDeferral: true);
    }

    [Fact]
    // JS awaits a .NET method via DotNet.invokeMethodAsync, non-streamed
    public void JSInterop_JSToDotNet_DoesNotPause_WhileCallInFlight()
    {
        NavigateToJSInteropPage();
        RunDeferralTest("js-to-dotnet-button", expectDeferral: true);
    }

    [Fact]
    // .NET to JS call with a large byte[] argument, sends a single non-streamed SignalR message
    public void JSInterop_LargeArgumentToJS_DoesNotPause_WhileCallInFlight()
    {
        NavigateToJSInteropPage();
        RunDeferralTest("large-arg-button", expectDeferral: true);
    }

    [Fact]
    // JS to .NET call with a large byte[] argument, symmetric to LargeArgumentToJS
    public void JSInterop_JSToDotNet_LargeArgument_DoesNotPause_WhileCallInFlight()
    {
        NavigateToJSInteropPage();
        RunDeferralTest("large-arg-from-js-button", expectDeferral: true);
    }

    [Fact]
    // No Blazor binding, so no preservation, deferring the pause instead.
    public void Typing_NativeInputFocused_DefersAutoPause()
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () =>
            {
                FocusElement("native-input");
                TypeInto("native-input", "abc");
            },
            expectDeferral: true);
    }

    [Fact]
    // No Blazor binding, so no preservation, deferring the pause instead.
    public void Typing_NativeTextareaFocused_DefersAutoPause()
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () =>
            {
                FocusElement("native-textarea");
                TypeInto("native-textarea", "abc");
            },
            expectDeferral: true);
    }

    [Fact]
    // No Blazor binding, so no preservation, deferring the pause instead.
    public void Typing_ContentEditableFocused_DefersAutoPause()
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () =>
            {
                FocusElement("content-editable");
                TypeInto("content-editable", "abc");
            },
            expectDeferral: true);
    }

    [Fact]
    public void Typing_PrefilledContentEditableFocusedNotEdited_PausesNormally()
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () => FocusElement("content-editable"),
            expectDeferral: false);
    }

    [Fact]
    // Blazor's <InputText> component goes through the InputBase/EditContext
    // pipeline rather than a raw <input> with @bind.
    public void Typing_BlazorInputTextFocused_DefersAutoPause()
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () =>
            {
                FocusElement("blazor-input-text");
                TypeInto("blazor-input-text", "abc");
            },
            expectDeferral: true);
    }

    [Theory]
    [InlineData("changed-value", true)]   // user typed something different from server default
    [InlineData("server-value", false)]   // user typed and ended up back at the server-provided value
    public void Typing_ServerProvidedDefaultInput_DefersOnlyWhenDirty(string finalValue, bool expectDeferral)
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () =>
            {
                FocusElement("server-default-input");
                SetValueAndCommit("server-default-input", finalValue);
            },
            expectDeferral: expectDeferral);
    }

    [Fact]
    // <button> has focus but is not editable
    public void Typing_FocusedNonEditableButton_PausesNormally()
    {
        NavigateToTypingPage();
        RunTypingDeferralTest(
            setup: () => FocusElement("focusable-button"),
            expectDeferral: false);
    }

    [Fact]
    // Blur fires the change event.
    public void Pause_PreservesNativeInputValue_WhenBlurred()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () =>
            {
                TypeInto("native-input", "abc");
                BlurElement("native-input");
            },
            verifyPreservation: () => AssertElementValueEquals("native-input", "abc"));
    }

    [Fact]
    // Focus is irrelevant: `change` fires on click.
    public void Pause_PreservesRadioSelection_AcrossResume()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () => ClickElement("radio-a"),
            verifyPreservation: () =>
                Assert.True(Browser.Exists(By.Id("radio-a")).Selected,
                    "Radio selection should survive pause/resume."));
    }

    [Fact]
    // Focus is irrelevant: `change` fires on click.
    public void Pause_PreservesCheckboxState_AcrossResume()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () => ClickElement("checkbox"),
            verifyPreservation: () =>
                Assert.True(Browser.Exists(By.Id("checkbox")).Selected,
                    "Checkbox state should survive pause/resume."));
    }

    [Fact]
    // Focus is irrelevant for <select>: `change` fires when an option is picked.
    public void Pause_PreservesSelectChoice_AcrossResume()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () => SetValueAndCommit("select", "y"),
            verifyPreservation: () => AssertElementValueEquals("select", "y"));
    }

    [Fact]
    // Focus is irrelevant: `change` fires when the user releases the pointer after dragging.
    public void Pause_PreservesSliderValue_AcrossResume()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () => SetValueAndCommit("range", "75"),
            verifyPreservation: () => AssertElementValueEquals("range", "75"));
    }
    [Fact]
    public void Pause_PreservesUploadFilename_WithPersistentState()
    {
        NavigateToTypingPage();
        var tempFile = Path.Combine(Path.GetTempPath(), $"autopause-upload-{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "x");
        var expectedName = Path.GetFileName(tempFile);
        try
        {
            RunPauseResumePreservationTest(
                setup: () => Browser.Exists(By.Id("persist-upload")).SendKeys(tempFile),
                verifyPreservation: () =>
                {
                    Assert.Equal(expectedName, Browser.Exists(By.Id("persist-upload-label")).Text);
                });
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* best effort */ }
        }
    }

    [Fact]
    // Documents a not-supported usage: @bind + [PersistentState] on a native <input type=file>.
    public void Pause_BindWithPersistentStateOnFileInput_ThrowsInvalidStateError()
    {
        Navigate($"/subdir/persistent-state/auto-pause-file-bind-with-persistent-state?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));

        var tempFile = Path.Combine(Path.GetTempPath(), $"autopause-bug-{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "x");
        try
        {
            Browser.Exists(By.Id("broken-file")).SendKeys(tempFile);
            // Confirm the bound field actually captured something pre-pause (browser-masked path).
            Browser.NotEqual("", () => Browser.Exists(By.Id("broken-file-value")).Text);

            ClearBlazorLogs();
            SetVisibility("hidden");
            try
            {
                WaitForPausedUI();
                // On resume the framework tries to write the persisted data back into input.value,
                // which the browser rejects. The exception surfaces.
                SetVisibility("visible");
                WaitForBlazorLog("InvalidStateError");
                WaitForBlazorLog("may only be programmatically set to the empty string");
            }
            finally
            {
                try { SetVisibility("visible"); } catch { /* ignore */ }
            }
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* best effort */ }
        }
    }

    [Fact]
    // Focus is irrelevant: `change` fires when the dialog closes.
    public void Pause_PreservesColorValue_AcrossResume()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () => SetValueAndCommit("color", "#ff0000"),
            verifyPreservation: () => AssertElementValueEquals("color", "#ff0000"));
    }

    [Fact]
    // Focus is irrelevant: `change` fires when the picker closes.
    public void Pause_PreservesDateValue_AcrossResume()
    {
        NavigateToTypingPage();
        RunPauseResumePreservationTest(
            setup: () => SetValueAndCommit("date", "2024-01-15"),
            verifyPreservation: () => AssertElementValueEquals("date", "2024-01-15"));
    }

    [Fact]
    // Focus is irrelevant: `change` fires on date selection.
    public void Typing_DateInputFocusedAndDirty_DoesNotDeferAutoPause()
    {
        NavigateToTypingPage();
        FocusAndSetValueWithoutBlur("date", "2024-06-15");
        SetVisibility("hidden");
        try
        {
            WaitForPausedUI();
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
    }

    private void NavigateToAdvancedEditablePage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-advanced-editable?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void Typing_CustomElementShadowDomInputFocused_DefersAutoPause()
    {
        NavigateToAdvancedEditablePage();
        RunTypingDeferralTest(
            setup: () =>
            {
                var js = (IJavaScriptExecutor)Browser;
                js.ExecuteScript("autoPauseAdvanced.focusShadowInput();");
                js.ExecuteScript("var i = document.getElementById('shadow-host').shadowRoot.getElementById('shadow-input'); i.value = 'abc'; i.dispatchEvent(new Event('input', { bubbles: true }));");
            },
            expectDeferral: true);
    }

    [Fact]
    public void Typing_SameOriginIframeContentEditableFocused_DefersAutoPause()
    {
        NavigateToAdvancedEditablePage();
        RunTypingDeferralTest(
            setup: () =>
            {
                var js = (IJavaScriptExecutor)Browser;
                js.ExecuteScript("autoPauseAdvanced.focusIframeEditable();");
                js.ExecuteScript("var d = document.getElementById('editable-iframe').contentDocument.getElementById('inner'); d.textContent = 'changed'; d.dispatchEvent(new Event('input', { bubbles: true }));");
            },
            expectDeferral: true);
    }

    [Fact]
    public void Typing_PrefilledIframeContentEditableClearedByUser_DefersAutoPause()
    {
        NavigateToAdvancedEditablePage();
        RunTypingDeferralTest(
            setup: () =>
            {
                var js = (IJavaScriptExecutor)Browser;
                js.ExecuteScript("autoPauseAdvanced.focusIframeEditable();");
                js.ExecuteScript("var d = document.getElementById('editable-iframe').contentDocument.getElementById('inner'); d.textContent = ''; d.dispatchEvent(new Event('input', { bubbles: true }));");
            },
            expectDeferral: true);
    }

    private void NavigateToMediaPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-media?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void Media_AudibleVideoPlaying_DefersAutoPause()
    {
        NavigateToMediaPage();
        Browser.Exists(By.Id("start-video")).Click();
        // Wait until the element actually reports playback so we don't race the pause check.
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "var v = document.getElementById('media-video'); return v && !v.paused;"));
        RunMediaDeferralTest(expectDeferral: true);
    }

    [Fact]
    public void Media_AudibleVideoPlayingInsideShadowRoot_DefersAutoPause()
    {
        Navigate($"/subdir/persistent-state/auto-pause-shadow-media?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
        Browser.Exists(By.Id("start-video")).Click();
        // The video lives in a shadow root, so query playback through the page's helper.
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.autoPauseShadowMedia && window.autoPauseShadowMedia.isPlaying();"));
        RunMediaDeferralTest(expectDeferral: true);
    }

    [Fact]
    public void Media_NothingPlaying_PausesNormally()
    {
        NavigateToMediaPage();
        RunMediaDeferralTest(expectDeferral: false);
    }

    [Fact]
    public void Media_MutedVideoPlaying_PausesNormally()
    {
        NavigateToMediaPage();
        Browser.Exists(By.Id("start-video")).Click();
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "var v = document.getElementById('media-video'); return v && !v.paused;"));
        Browser.Exists(By.Id("mute-video")).Click();
        RunMediaDeferralTest(expectDeferral: false);
    }

    [Fact]
    public void PictureInPicture_Active_DefersAutoPause()
    {
        NavigateToMediaPage();
        StartPlaybackAndWaitForMetadata();
        EnterPictureInPicture();
        Browser.Exists(By.Id("pause-video")).Click();
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "var v = document.getElementById('media-video'); return v && v.paused;"));
        RunPictureInPictureDeferralTest();
    }

    [Fact]
    public void PictureInPicture_ClosedWhileHidden_PausesAfterDefer()
    {
        NavigateToMediaPage();
        StartPlaybackAndWaitForMetadata();
        EnterPictureInPicture();
        Browser.Exists(By.Id("pause-video")).Click();

        SetVisibility("hidden");
        try
        {
            AssertPauseStaysDeferred();
            Browser.Exists(By.Id("pip-exit")).Click();
            WaitForPausedUI();
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
    }

    private void StartPlaybackAndWaitForMetadata()
    {
        Browser.Exists(By.Id("start-video")).Click();
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "var v = document.getElementById('media-video'); return v && !v.paused && v.readyState >= 1 && v.videoWidth > 0;"));
    }

    private void EnterPictureInPicture()
    {
        Browser.Exists(By.Id("pip-enter")).Click();
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "var v = document.getElementById('media-video'); return document.pictureInPictureElement === v;"));
    }

    private void RunPictureInPictureDeferralTest()
    {
        SetVisibility("hidden");
        try
        {
            AssertPauseStaysDeferred();
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
    }

    private void NavigateToWebLockPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-weblock?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void WebLock_HeldThenReleased_DefersAndPauses()
    {
        NavigateToWebLockPage();
        Browser.Exists(By.Id("acquire-lock")).Click();
        Browser.True(() => Browser.FindElement(By.Id("lock-status")).Text == "held");

        SetVisibility("hidden");
        try
        {
            AssertPauseStaysDeferred();
            Browser.Exists(By.Id("release-lock")).Click();
            Browser.True(() => Browser.FindElement(By.Id("lock-status")).Text == "released");
            WaitForPausedUI();
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
    }

    private void NavigateToFetchPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-fetch?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void Fetch_InFlightDuringPause_DeliversResultOnReturn()
    {
        NavigateToFetchPage();
        var token = Browser.Exists(By.Id("start-fetch")).GetDomAttribute("data-token");

        Browser.Exists(By.Id("start-fetch")).Click();
        WaitForStreamStarted(token);
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__fetchResult") as string == "pending");

        SetVisibility("hidden");
        WaitForPausedUI();

        ReleaseGate(token);

        SetVisibility("visible");
        WaitForResumedUI();

        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__fetchResult") as string == "delivered");
    }

    private void NavigateToWebSocketPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-websocket?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void WebSocket_MessageDuringPause_DeliversResultOnReturn()
    {
        NavigateToWebSocketPage();
        var token = Browser.Exists(By.Id("start-ws")).GetDomAttribute("data-token");

        Browser.Exists(By.Id("start-ws")).Click();
        WaitForStreamStarted(token);
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__wsResult") as string == "pending");

        SetVisibility("hidden");
        WaitForPausedUI();

        ReleaseGate(token);

        SetVisibility("visible");
        WaitForResumedUI();

        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__wsResult") as string == "delivered");
    }

    private void NavigateToServiceWorkerPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-serviceworker?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__swReady === true") is true);
    }

    [Fact]
    public void ServiceWorker_EventDuringPause_DeliversResultOnReturn()
    {
        NavigateToServiceWorkerPage();
        var token = Browser.Exists(By.Id("start-sw")).GetDomAttribute("data-token");

        Browser.Exists(By.Id("start-sw")).Click();
        WaitForStreamStarted(token);
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__swResult") as string == "pending");

        SetVisibility("hidden");
        WaitForPausedUI();

        ReleaseGate(token);

        SetVisibility("visible");
        WaitForResumedUI();

        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__swResult") as string == "delivered");
    }

    private void NavigateToIndexedDBPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-indexeddb?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
    }

    [Fact]
    public void IndexedDB_TransactionDuringPause_DeliversResultOnReturn()
    {
        NavigateToIndexedDBPage();
        var token = Browser.Exists(By.Id("start-idb")).GetDomAttribute("data-token");

        Browser.Exists(By.Id("start-idb")).Click();
        WaitForStreamStarted(token);
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__idbResult") as string == "pending");

        SetVisibility("hidden");
        WaitForPausedUI();

        ReleaseGate(token);

        SetVisibility("visible");
        WaitForResumedUI();

        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__idbResult") as string == "delivered");
    }

    private void NavigateToBackgroundSyncPage()
    {
        Navigate($"/subdir/persistent-state/auto-pause-background-sync?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__swReady === true") is true);
    }

    [Fact]
    public void BackgroundSync_EventDuringPause_DeliversResultOnReturn()
    {
        NavigateToBackgroundSyncPage();
        var token = Browser.Exists(By.Id("start-sync")).GetDomAttribute("data-token");

        Browser.Exists(By.Id("start-sync")).Click();
        WaitForStreamStarted(token);
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__syncResult") as string == "pending");

        SetVisibility("hidden");
        WaitForPausedUI();

        ReleaseGate(token);

        SetVisibility("visible");
        WaitForResumedUI();

        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript("return window.__syncResult") as string == "delivered");
    }

    [Fact]
    public void AbortSignal_IsAborted_WhenTabBecomesVisibleDuringCallback()
    {
        NavigateToTypingPage();
        InstallHangingAbortHook();
        try
        {
            SetVisibility("hidden");
            WaitForAbortHookStarted();

            SetVisibility("visible");

            AssertAbortSignalFired();
            AssertSignalWasNotAbortedOnEntry();
        }
        finally
        {
            UninstallHangingAbortHook();
        }
    }

    [Fact]
    public void AbortSignal_IsAborted_WhenSupersededByNewServerPause()
    {
        NavigateToServerPausePage();
        InstallHangingAbortHookForServerPause();
        try
        {
            TriggerServerPause();
            WaitForServerAbortHookStarted(expectedCount: 1);

            TriggerServerPause();

            AssertServerAbortSignalFired(index: 0, expectedReason: "superseded by new pause request");
        }
        finally
        {
            UninstallHangingAbortHookForServerPause();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomElement_UnboundDirtyInputs_StatePreservedOnlyWithMitigation(bool useMitigation)
    {
        RunRiskMitigationTest(
            useMitigation,
            page: "auto-pause-custom-element-risk",
            setupScript: @"
                var shadow = document.getElementById('custom-form').shadowRoot;
                shadow.getElementById('f1').value = 'aaa';
                shadow.getElementById('f2').value = 'bbb';
                shadow.getElementById('f3').focus();",
            savedVar: "__savedFormValues",
            expectedWhenMitigated: "aaa,bbb,");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Canvas_DrawingState_PreservedOnlyWithMitigation(bool useMitigation)
    {
        RunRiskMitigationTest(
            useMitigation,
            page: "auto-pause-canvas-risk",
            setupScript: @"
                var ctx = document.getElementById('test-canvas').getContext('2d');
                ctx.fillStyle = 'red';
                ctx.fillRect(0, 0, 50, 50);",
            savedVar: "__savedCanvasData",
            assertMitigated: saved =>
            {
                Assert.NotNull(saved);
                Assert.StartsWith("data:image/png", saved);
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosedShadowDOM_InternalInputs_StatePreservedOnlyWithMitigation(bool useMitigation)
    {
        RunRiskMitigationTest(
            useMitigation,
            page: "auto-pause-closed-shadow-risk",
            setupScript: "document.getElementById('closed-form').setValues('secret1', 'secret2')",
            savedVar: "__savedClosedFormValues",
            expectedWhenMitigated: "secret1,secret2");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossOriginIframe_InternalState_PreservedOnlyWithMitigation(bool useMitigation)
    {
        var mitigationParam = useMitigation ? "&use-mitigation=true" : "";
        Navigate($"/subdir/persistent-state/auto-pause-cross-origin-iframe-risk?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}{mitigationParam}");
        Browser.Exists(By.Id("render-mode-interactive"));
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__iframeReady === true"));

        RunRiskMitigationTest(
            useMitigation,
            savedVar: "__savedIframeState",
            expectedWhenMitigated: "iframe-state-42");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WebRTC_ConnectionState_PreservedOnlyWithMitigation(bool useMitigation)
    {
        var mitigationParam = useMitigation ? "&use-mitigation=true" : "";
        Navigate($"/subdir/persistent-state/auto-pause-webrtc-risk?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}{mitigationParam}");
        Browser.Exists(By.Id("render-mode-interactive"));
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript("return window.__rtcReady === true"));

        RunRiskMitigationTest(
            useMitigation,
            savedVar: "__savedWebRTCState",
            expectedWhenMitigated: "connected");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MediaCapture_StreamState_PreservedOnlyWithMitigation(bool useMitigation)
    {
        RunRiskMitigationTest(
            useMitigation,
            page: "auto-pause-media-capture-risk",
            savedVar: "__savedMediaState",
            expectedWhenMitigated: "true,1,live");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void XRSession_State_PreservedOnlyWithMitigation(bool useMitigation)
    {
        RunRiskMitigationTest(
            useMitigation,
            page: "auto-pause-xr-risk",
            savedVar: "__savedXRState",
            expectedWhenMitigated: "immersive-vr,visible,false");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Notification_ActiveState_PreservedOnlyWithMitigation(bool useMitigation)
    {
        RunRiskMitigationTest(
            useMitigation,
            page: "auto-pause-notification-risk",
            savedVar: "__savedNotificationState",
            expectedWhenMitigated: "msg-1,reminder-2");
    }

    private void RunRiskMitigationTest(
        bool useMitigation,
        string savedVar,
        string page = null,
        string setupScript = null,
        string expectedWhenMitigated = null,
        Action<string> assertMitigated = null)
    {
        if (page != null)
        {
            var mitigationParam = useMitigation ? "&use-mitigation=true" : "";
            Navigate($"/subdir/persistent-state/{page}?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}{mitigationParam}");
            Browser.Exists(By.Id("render-mode-interactive"));
        }

        if (useMitigation)
        {
            WaitForBlazorPause();
            ((IJavaScriptExecutor)Browser).ExecuteScript(
                "Blazor.pause.waitFor(window.__mitigationHandler)");
        }

        if (setupScript != null)
        {
            ((IJavaScriptExecutor)Browser).ExecuteScript(setupScript);
        }

        SetVisibility("hidden");
        WaitForPausedUI();

        SetVisibility("visible");
        WaitForResumedUI();

        var saved = ((IJavaScriptExecutor)Browser).ExecuteScript($"return window.{savedVar}") as string;
        if (useMitigation)
        {
            if (assertMitigated != null)
            {
                assertMitigated(saved);
            }
            else
            {
                Assert.Equal(expectedWhenMitigated, saved);
            }
        }
        else
        {
            Assert.Null(saved);
        }
    }

    private void RunMediaDeferralTest(bool expectDeferral)
    {
        SetVisibility("hidden");
        try
        {
            if (expectDeferral)
            {
                AssertPauseStaysDeferred();
            }
            else
            {
                WaitForPausedUI();
            }
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
    }

    private void RunDeferralTest(string elementId, bool expectDeferral)
    {
        var token = ReadToken(elementId);
        RunDeferralTest(token, () => Browser.Exists(By.Id(elementId)).Click(), expectDeferral);
    }

    private void RunTypingDeferralTest(Action setup, bool expectDeferral, Action verifyPreservation = null)
    {
        setup();
        SetVisibility("hidden");
        try
        {
            if (expectDeferral)
            {
                AssertPauseStaysDeferred();
            }
            else
            {
                WaitForPausedUI();
            }
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
        verifyPreservation?.Invoke();
    }

    private void RunPauseResumePreservationTest(Action setup, Action verifyPreservation)
    {
        setup();
        SetVisibility("hidden");
        try
        {
            WaitForPausedUI();
        }
        finally
        {
            SetVisibility("visible");
            WaitForResumedUI();
        }
        verifyPreservation();
    }

    private void AssertElementValueEquals(string elementId, string expected)
    {
        var actual = Browser.Exists(By.Id(elementId)).GetDomProperty("value");
        Assert.Equal(expected, actual);
    }

    private void FocusElement(string elementId)
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "document.getElementById(arguments[0]).focus();", elementId);
    }

    private void BlurElement(string elementId)
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "document.getElementById(arguments[0]).blur();", elementId);
    }

    private void TypeInto(string elementId, string text)
    {
        var element = Browser.Exists(By.Id(elementId));
        element.SendKeys(text);
    }

    private void FocusAndSetValueWithoutBlur(string elementId, string value)
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            var el = document.getElementById(arguments[0]);
            el.focus();
            el.value = arguments[1];
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));
        ", elementId, value);
    }

    private void ClickElement(string elementId)
    {
        Browser.Exists(By.Id(elementId)).Click();
    }

    private void SetValueAndCommit(string elementId, string value)
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            var el = document.getElementById(arguments[0]);
            el.value = arguments[1];
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));
        ", elementId, value);
    }

    private void RunDeferralTest(string token, Action triggerAction, bool expectDeferral)
    {
        triggerAction();
        WaitForStreamStarted(token);

        ClearBlazorLogs();
        SetVisibility("hidden");

        try
        {
            if (expectDeferral)
            {
                AssertPauseStaysDeferred();
            }
            else
            {
                WaitForPausedUI();
            }
        }
        finally
        {
            ReleaseGate(token);
        }

        if (expectDeferral)
        {
            // Modal only appears after the drain releases the deferred pause.
            WaitForPausedUI();
        }

        AssertNoConsoleErrors();

        SetVisibility("visible");
        WaitForResumedUI();
    }

    private string ReadToken(string elementId)
    {
        var token = Browser.Exists(By.Id(elementId)).GetDomAttribute("data-token");
        Assert.False(string.IsNullOrEmpty(token), $"Element {elementId} is missing a data-token attribute.");
        return token!;
    }

    private bool IsReconnectModalShown()
    {
        var modals = Browser.FindElements(By.Id("components-reconnect-modal"));
        if (modals.Count == 0)
        {
            return false;
        }
        var display = modals[0].GetCssValue("display");
        return display == "block";
    }

    // The framework emits no deterministic "pause deferred" signal, so deferral is proven by
    // observing that the reconnect modal (shown only once the circuit pauses) stays hidden for a
    // window comfortably longer than the configured pause delay while the blocking condition holds.
    private static readonly TimeSpan DeferralObservationWindow = TimeSpan.FromMilliseconds(PauseDelayMs * 5);

    private void AssertPauseStaysDeferred()
    {
        var deadline = DateTime.UtcNow + DeferralObservationWindow;
        while (DateTime.UtcNow < deadline)
        {
            Assert.False(IsReconnectModalShown(),
                "The reconnect modal is showing — pause should be held while the deferral condition is active.");
            Thread.Sleep(50);
        }
    }

    private void NavigateToServerPausePage()
    {
        Navigate($"/subdir/persistent-state/server-pause?auto-pause=true&auto-pause-delay-ms={PauseDelayMs}");
        Browser.Exists(By.Id("render-mode-interactive"));
        WaitForBlazorPause();
    }

    private void InstallHangingAbortHook()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            window.__abortResult = null;
            window.__abortHandler = (signal) => new Promise((resolve) => {
                window.__abortResult = { started: true, abortedOnEntry: signal.aborted };
                signal.addEventListener('abort', () => {
                    window.__abortResult.abortedLater = true;
                    resolve();
                });
            });
            window.__abortUnsub = Blazor.pause.waitFor(window.__abortHandler);
        ");
    }

    private void UninstallHangingAbortHook()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            if (window.__abortHandler) {
                window.__abortUnsub && window.__abortUnsub();
                window.__abortUnsub = null;
                window.__abortHandler = null;
            }
        ");
    }

    private void WaitForAbortHookStarted()
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.__abortResult !== null && window.__abortResult.started === true"));
    }

    private void AssertAbortSignalFired()
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.__abortResult.abortedLater === true"));
    }

    private void AssertSignalWasNotAbortedOnEntry()
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.__abortResult.abortedOnEntry === false"));
    }

    private void InstallHangingAbortHookForServerPause()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            window.__serverAbortResults = [];
            window.__serverAbortHandler = (signal) => new Promise((resolve) => {
                const idx = window.__serverAbortResults.length;
                window.__serverAbortResults.push({ started: true, abortedOnEntry: signal.aborted });
                signal.addEventListener('abort', () => {
                    window.__serverAbortResults[idx].abortedLater = true;
                    window.__serverAbortResults[idx].reason = signal.reason;
                    resolve();
                });
            });
            window.__serverAbortUnsub = Blazor.pause.waitFor(window.__serverAbortHandler, { source: 'server' });
        ");
    }

    private void UninstallHangingAbortHookForServerPause()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript(@"
            if (window.__serverAbortHandler) {
                window.__serverAbortUnsub && window.__serverAbortUnsub();
                window.__serverAbortUnsub = null;
                window.__serverAbortHandler = null;
            }
        ");
    }

    private void TriggerServerPause()
    {
        var circuitId = Browser.Exists(By.Id("circuit-id")).Text;
        ((IJavaScriptExecutor)Browser).ExecuteScript(
            $"DotNet.invokeMethodAsync('Components.TestServer', 'TriggerServerPause', '{circuitId}')");
    }

    private void WaitForServerAbortHookStarted(int expectedCount)
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            $"return window.__serverAbortResults.length === {expectedCount} && window.__serverAbortResults[{expectedCount - 1}].started === true"));
    }

    private void AssertServerAbortSignalFired(int index, string expectedReason)
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            $"return window.__serverAbortResults[{index}].abortedLater === true"));
        Browser.True(() => ((IJavaScriptExecutor)Browser).ExecuteScript(
            $"return window.__serverAbortResults[{index}].reason") as string == expectedReason);
    }

    private void SetVisibility(string state)
    {
        var js = (IJavaScriptExecutor)Browser;
        js.ExecuteScript($@"
            Object.defineProperty(document, 'visibilityState', {{ configurable: true, get: () => '{state}' }});
            Object.defineProperty(document, 'hidden', {{ configurable: true, get: () => {(state == "hidden" ? "true" : "false")} }});
            document.dispatchEvent(new Event('visibilitychange'));
        ");
    }

    private static readonly TimeSpan LogWaitTimeout = TimeSpan.FromSeconds(10);

    private void WaitForBlazorPause()
    {
        Browser.True(() => (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return !!(window.Blazor && Blazor.pause && typeof Blazor.pause.waitFor === 'function')"));
    }

    private void ClearBlazorLogs()
    {
        ((IJavaScriptExecutor)Browser).ExecuteScript("window.__blazorLogs && (window.__blazorLogs.length = 0);");
    }

    private void WaitForBlazorLog(string substring)
    {
        var deadline = DateTime.UtcNow + LogWaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var found = (bool)((IJavaScriptExecutor)Browser).ExecuteScript(
                "var s = arguments[0]; return !!(window.__blazorLogs && window.__blazorLogs.some(function (e) { return e.msg && e.msg.indexOf(s) >= 0; }));",
                substring);
            if (found)
            {
                return;
            }
            Thread.Sleep(25);
        }
        throw new TimeoutException($"Timed out after {LogWaitTimeout.TotalSeconds}s waiting for Blazor log line containing: \"{substring}\".");
    }

    private void WaitForPausedUI()
    {
        Browser.Equal("block", () =>
            Browser.Exists(By.Id("components-reconnect-modal")).GetCssValue("display"));
    }

    private void WaitForResumedUI()
    {
        // After visibility returns the modal is hidden again (display != "block").
        Browser.NotEqual("block", () =>
        {
            var modals = Browser.FindElements(By.Id("components-reconnect-modal"));
            return modals.Count == 0 ? "none" : modals[0].GetCssValue("display");
        });
    }

    private void AssertNoConsoleErrors()
    {
        var severeEntries = Browser.Manage().Logs.GetLog(LogType.Browser)
            .Where(e => e.Level == OpenQA.Selenium.LogLevel.Severe)
            // Only flag the SignalR-rejection symptom we care about; other
            // unrelated Severe entries are out of scope for this test.
            .Where(e => e.Message.Contains("Cannot send data", StringComparison.OrdinalIgnoreCase)
                     || e.Message.Contains("HubException", StringComparison.OrdinalIgnoreCase)
                     || e.Message.Contains("Invocation canceled", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(severeEntries.Count == 0,
            "Browser console reported SignalR-related errors after the gated download:\n  " +
            string.Join("\n  ", severeEntries.Select(e => e.Message)));
    }

    private void WaitForStreamStarted(string token)
        => PollServerFlag(token, "started", "stream did not start");

    private void PollServerFlag(string token, string flagName, string failureMessage)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        using var http = NewHttpClient();
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = http.GetAsync($"/subdir/autopause-test/{flagName}/{token}").GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var payload = response.Content.ReadFromJsonAsync<Dictionary<string, bool>>().GetAwaiter().GetResult();
                    if (payload != null && payload.TryGetValue(flagName, out var value) && value)
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Server may briefly be unreachable; retry.
            }
            Thread.Sleep(50);
        }
        throw new TimeoutException($"Timed out after 30s waiting for {flagName} flag on token {token}: {failureMessage}.");
    }

    private void ReleaseGate(string token)
    {
        using var http = NewHttpClient();
        using var response = http.PostAsync($"/subdir/autopause-test/release/{token}", content: null).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
    }

    private HttpClient NewHttpClient()
    {
        var serverRoot = new Uri(Browser.Url).GetLeftPart(UriPartial.Authority);
        return new HttpClient { BaseAddress = new Uri(serverRoot) };
    }
}
