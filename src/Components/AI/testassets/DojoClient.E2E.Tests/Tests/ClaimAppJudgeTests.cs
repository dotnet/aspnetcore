// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

// Only the ClaimApp model backend is replaced. The browser still exercises the app's
// AGUIChatClient, HTTP/SSE endpoint, protocol serialization, and Components.AI pipeline.
[UITest]
public partial class ClaimAppJudgeTests : BrowserTest
{
    private const string TestJpegBase64 =
        "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/wAALCAABAAEBAREA/8QAFAABAAAAAAAAAAAAAAAAAAAAAP/EABQQAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQEAAD8AN//Z";

    private readonly List<string> _pageErrors = [];
    private ServerInstance _app = null!;
    private IPage _page = null!;
    private string _testUrl = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        var externalUrl = Environment.GetEnvironmentVariable("CLAIM_APP_TEST_URL");
        IBrowserContext context;
        if (string.IsNullOrWhiteSpace(externalUrl))
        {
            _app = await StartServerAsync<global::ComponentsAIClaimApp.Components.App>(
                TestRoot.Servers,
                options => options.ConfigureServices<ClaimAppModelOverrides>(
                    nameof(ClaimAppModelOverrides.UseTestModel)));
            _testUrl = _app.TestUrl;
            context = await NewContext(
                new BrowserNewContextOptions().WithServerRouting(_app));
        }
        else
        {
            _testUrl = externalUrl;
            context = await NewContext(new BrowserNewContextOptions());
        }

        _page = await context.NewPageAsync();
        _page.PageError += (_, exception) =>
            _pageErrors.Add($"Page error: {exception}");
    }

    [TestMethod]
    public async Task ClaimFlow_IsAccessibleAndCompletes()
    {
        await GoToAppAsync();

        await Expect(_page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Vehicle damage assessment", Level = 1 }))
            .ToBeVisibleAsync();
        await Expect(_page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Damage assessment agent", Level = 2 }))
            .ToBeVisibleAsync();
        await Expect(ClaimDescription).ToBeEnabledAsync();
        await Expect(_page.Locator("input[type=file]"))
            .ToHaveCSSAsync("opacity", "0");
        await Expect(_page.GetByRole(
            AriaRole.Group,
            new() { Name = "Color theme" }))
            .ToBeVisibleAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Front impact", Exact = true }).ClickAsync();
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Send", Exact = true }).ClickAsync();

        var approveButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve assessment", Exact = true });
        await Expect(approveButton).ToBeVisibleAsync(new() { Timeout = 20_000 });
        await Expect(ClaimDetail("Confidence")).ToHaveTextAsync("78%");
        await approveButton.ClickAsync();

        await Expect(ClaimDetail("Decision"))
            .ToHaveTextAsync("Approved", new() { Timeout = 15_000 });
        await Expect(_page.GetByText(
            "The assessment is approved and ready for the next claim step.",
            new() { Exact = true }))
            .ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task Greeting_StaysInIntakeWithoutAssessmentApproval()
    {
        await GoToAppAsync();

        await ClaimDescription.FillAsync("hey");
        await SendButton.ClickAsync();

        await Expect(_page.GetByText(
            "Hi. I can inspect vehicle photos",
            new() { Exact = false }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(_page.Locator(".claim-chat-header").GetByRole(AriaRole.Status))
            .ToContainTextAsync("Ready for claim details");
        await Expect(_page.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve assessment", Exact = true }))
            .ToHaveCountAsync(0);
        await Expect(ClaimDescription).ToBeEnabledAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task ConversationalPrompt_UsesConfiguredModel()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")))
        {
            Assert.Inconclusive("The Foundry claim endpoint is not configured.");
        }

        await GoToAppAsync();
        await ClaimDescription.FillAsync("OK, so can you hear me?");
        await SendButton.ClickAsync();

        var assistantText = _page
            .Locator(".sc-ai-turn .sc-ai-message--assistant .sc-ai-message__content")
            .Last;
        await Expect(assistantText)
            .ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(assistantText)
            .Not.ToContainTextAsync("I need a little more claim detail");
        await Expect(assistantText)
            .ToContainTextAsync(
                new System.Text.RegularExpressions.Regex(
                    "hear|read|message",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task LiveVoice_ShowsTwoResponsesWithoutPlaybackAndResumesUntilStopped()
    {
        await _page.AddInitScriptAsync(
            """
            globalThis.claimVoiceEvents = [];
            globalThis.claimSpokenTexts = [];

            class ClaimTurnSpeechRecognition extends EventTarget {
                static nextTranscript = 0;
                static transcripts = [
                    "OK, so can you hear me?",
                    "What can you do?"
                ];

                start() {
                    this.active = true;
                    globalThis.claimVoiceEvents.push("recognition:start");
                    this.dispatchEvent(new Event("start"));
                    const index = ClaimTurnSpeechRecognition.nextTranscript;
                    if (index >= ClaimTurnSpeechRecognition.transcripts.length) {
                        return;
                    }

                    ClaimTurnSpeechRecognition.nextTranscript++;
                    this.timer = setTimeout(() => {
                        if (!this.active) {
                            return;
                        }

                        const result = [{
                            transcript: ClaimTurnSpeechRecognition.transcripts[index]
                        }];
                        result.isFinal = true;
                        const event = new Event("result");
                        event.resultIndex = 0;
                        event.results = [result];
                        this.dispatchEvent(event);
                    }, 100);
                }

                stop() {
                    if (!this.active) {
                        return;
                    }

                    this.active = false;
                    clearTimeout(this.timer);
                    globalThis.claimVoiceEvents.push("recognition:stop");
                    this.dispatchEvent(new Event("end"));
                }

                abort() {
                    this.stop();
                }
            }

            globalThis.SpeechRecognition = ClaimTurnSpeechRecognition;
            speechSynthesis.speak = utterance => {
                globalThis.claimSpokenTexts.push(utterance.text);
            };
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Start live voice", Exact = true }).ClickAsync();

        var assistantMessages =
            _page.Locator(".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(assistantMessages).ToHaveCountAsync(
            2,
            new() { Timeout = 120_000 });
        await _page.WaitForFunctionAsync(
            """
            () => globalThis.claimVoiceEvents
                .filter(event => event === "recognition:start").length >= 3
            """,
            null,
            new() { Timeout = 15_000 });

        Assert.HasCount(
            0,
            await _page.EvaluateAsync<string[]>(
                "() => globalThis.claimSpokenTexts"));
        var startCount = await _page.EvaluateAsync<int>(
            """
            () => globalThis.claimVoiceEvents
                .filter(event => event === "recognition:start").length
            """);
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop live voice", Exact = true }).ClickAsync();
        await Expect(_page.Locator(".sc-ai-input__live-speech"))
            .ToHaveAttributeAsync("aria-pressed", "false");
        await _page.WaitForTimeoutAsync(750);
        Assert.AreEqual(
            startCount,
            await _page.EvaluateAsync<int>(
                """
                () => globalThis.claimVoiceEvents
                    .filter(event => event === "recognition:start").length
                """));
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task LiveVoice_RecoversFromTransientRecognitionErrorsUntilStopped()
    {
        await _page.AddInitScriptAsync(
            """
            globalThis.claimSpeechStartCount = 0;

            class RecoveringSpeechRecognition extends EventTarget {
                start() {
                    this.active = true;
                    globalThis.claimSpeechStartCount++;
                    this.dispatchEvent(new Event("start"));
                    if (globalThis.claimSpeechStartCount !== 1) {
                        return;
                    }

                    setTimeout(() => {
                        this.active = false;
                        const error = new Event("error");
                        Object.defineProperty(error, "error", { value: "network" });
                        this.dispatchEvent(error);
                        this.dispatchEvent(new Event("end"));
                    }, 100);
                }

                stop() {
                    this.active = false;
                    this.dispatchEvent(new Event("end"));
                }

                abort() {
                    this.active = false;
                    this.dispatchEvent(new Event("end"));
                }
            }

            globalThis.SpeechRecognition = RecoveringSpeechRecognition;
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Start live voice", Exact = true }).ClickAsync();
        await _page.WaitForFunctionAsync(
            "() => globalThis.claimSpeechStartCount >= 2",
            null,
            new() { Timeout = 5_000 });

        var stopLiveVoice = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop live voice", Exact = true });
        await Expect(stopLiveVoice).ToBeVisibleAsync();
        await Expect(_page.GetByText(
            "Live voice stopped because speech recognition failed.",
            new() { Exact = true })).ToHaveCountAsync(0);

        var startCount = await _page.EvaluateAsync<int>(
            "() => globalThis.claimSpeechStartCount");
        await stopLiveVoice.ClickAsync();
        await Expect(_page.Locator(".sc-ai-input__live-speech"))
            .ToHaveAttributeAsync("aria-pressed", "false");
        await _page.WaitForTimeoutAsync(750);
        Assert.AreEqual(
            startCount,
            await _page.EvaluateAsync<int>(
                "() => globalThis.claimSpeechStartCount"));
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task LiveVoice_StopDuringDelayedStartAbortsWithoutReactivating()
    {
        await _page.AddInitScriptAsync(
            """
            globalThis.claimDelayedStartEvents = [];

            class DelayedStartSpeechRecognition extends EventTarget {
                start() {
                    globalThis.claimDelayedStartEvents.push("start-requested");
                    setTimeout(() => {
                        globalThis.claimDelayedStartEvents.push("start");
                        this.dispatchEvent(new Event("start"));
                    }, 300);
                }

                stop() {
                    globalThis.claimDelayedStartEvents.push("stop");
                    this.dispatchEvent(new Event("end"));
                }

                abort() {
                    globalThis.claimDelayedStartEvents.push("abort");
                    this.dispatchEvent(new Event("end"));
                }
            }

            globalThis.SpeechRecognition = DelayedStartSpeechRecognition;
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Start live voice", Exact = true }).ClickAsync();
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop live voice", Exact = true }).ClickAsync();
        await _page.WaitForTimeoutAsync(750);

        await Expect(_page.Locator(".sc-ai-input__live-speech"))
            .ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(ClaimDescription).ToBeEnabledAsync();
        await Expect(_page.Locator(".sc-ai-input__status"))
            .ToContainTextAsync("Live voice stopped.");
        Assert.IsTrue(
            await _page.EvaluateAsync<int>(
                """
                () => globalThis.claimDelayedStartEvents
                    .filter(event => event === "abort").length
                """) >= 2);
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task LiveVoice_BackendErrorResumesWithANewTurn()
    {
        await _page.AddInitScriptAsync(
            """
            class BackendErrorSpeechRecognition extends EventTarget {
                static nextTranscript = 0;
                static transcripts = [
                    "Trigger a live error while assessing the front bumper.",
                    "The rear bumper is damaged."
                ];

                start() {
                    this.active = true;
                    this.dispatchEvent(new Event("start"));
                    const index = BackendErrorSpeechRecognition.nextTranscript++;
                    if (index >= BackendErrorSpeechRecognition.transcripts.length) {
                        return;
                    }

                    setTimeout(() => {
                        if (!this.active) {
                            return;
                        }

                        const result = [{
                            transcript: BackendErrorSpeechRecognition.transcripts[index]
                        }];
                        result.isFinal = true;
                        const event = new Event("result");
                        event.resultIndex = 0;
                        event.results = [result];
                        this.dispatchEvent(event);
                    }, 100);
                }

                stop() {
                    this.active = false;
                    this.dispatchEvent(new Event("end"));
                }

                abort() {
                    this.active = false;
                    this.dispatchEvent(new Event("end"));
                }
            }

            globalThis.SpeechRecognition = BackendErrorSpeechRecognition;
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Start live voice", Exact = true }).ClickAsync();

        var userMessages =
            _page.Locator(".sc-ai-message--user .sc-ai-message__content");
        await Expect(userMessages).ToHaveCountAsync(2, new() { Timeout = 20_000 });
        await Expect(userMessages.First).ToHaveTextAsync(
            "Trigger a live error while assessing the front bumper.");
        await Expect(userMessages.Last).ToHaveTextAsync(
            "The rear bumper is damaged.");
        await Expect(_page.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve assessment", Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 20_000 });
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop live voice", Exact = true }).ClickAsync();
        await Expect(_page.Locator(".sc-ai-input__live-speech"))
            .ToHaveAttributeAsync("aria-pressed", "false");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task UnclearPhoto_RequestsEvidenceWithoutAssessmentApproval()
    {
        await GoToAppAsync();
        var imagePath = Path.Combine(
            Path.GetTempPath(),
            $"claim-unclear-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(
            imagePath,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));

        try
        {
            await _page.Locator("input[type=file]").SetInputFilesAsync(imagePath);
            await SendButton.ClickAsync();

            await Expect(_page.Locator(".claim-chat-header").GetByRole(AriaRole.Status))
                .ToContainTextAsync("More evidence needed", new() { Timeout = 15_000 });
            await Expect(_page.GetByRole(
                AriaRole.Button,
                new() { Name = "Approve assessment", Exact = true }))
                .ToHaveCountAsync(0);
            await Expect(ClaimDescription).ToBeEnabledAsync();
        }
        finally
        {
            File.Delete(imagePath);
        }

        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task AdditionalPhoto_ReassessesCumulativeEvidenceBeforeApproval()
    {
        await GoToAppAsync();
        var firstImagePath = Path.Combine(
            Path.GetTempPath(),
            $"claim-first-{Guid.NewGuid():N}.png");
        var secondImagePath = Path.Combine(
            Path.GetTempPath(),
            $"claim-second-{Guid.NewGuid():N}.png");
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        await File.WriteAllBytesAsync(firstImagePath, imageBytes);
        await File.WriteAllBytesAsync(secondImagePath, imageBytes);

        try
        {
            await ClaimDescription.FillAsync(
                "The front bumper is cracked and the hood is bent.");
            await _page.Locator("input[type=file]").SetInputFilesAsync(firstImagePath);
            await Expect(_page.Locator(".claim-composer__attachments img"))
                .ToHaveCountAsync(1);
            await SendButton.ClickAsync();

            await Expect(_page.GetByRole(
                AriaRole.Button,
                new() { Name = "Add more evidence", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
            await _page.GetByRole(
                AriaRole.Button,
                new() { Name = "Add more evidence", Exact = true }).ClickAsync();
            await Expect(ClaimDescription).ToBeEnabledAsync();

            await ClaimDescription.FillAsync(
                "This second angle also shows a broken left headlight.");
            await _page.Locator("input[type=file]").SetInputFilesAsync(secondImagePath);
            await Expect(_page.Locator(".claim-composer__attachments img"))
                .ToHaveCountAsync(1);
            await SendButton.ClickAsync();

            await Expect(ClaimDetail("Evidence"))
                .ToHaveTextAsync("2 images", new() { Timeout = 20_000 });
            await Expect(_page.Locator(".claim-photo-gallery figure"))
                .ToHaveCountAsync(2);
            var approveButton = _page.GetByRole(
                AriaRole.Button,
                new() { Name = "Approve assessment", Exact = true });
            await Expect(approveButton).ToBeVisibleAsync();
            await approveButton.ClickAsync();
            await Expect(ClaimDetail("Decision"))
                .ToHaveTextAsync("Approved", new() { Timeout = 15_000 });
        }
        finally
        {
            File.Delete(firstImagePath);
            File.Delete(secondImagePath);
        }

        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task Themes_SwitchAndPersist()
    {
        await GoToAppAsync();

        var darkButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Dark", Exact = true });
        await darkButton.ClickAsync();
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "dark");
        await Expect(darkButton).ToHaveAttributeAsync("aria-pressed", "true");

        await _page.ReloadAsync();
        await _page.WaitForInteractiveAsync(".claim-composer__textarea");
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "dark");

        var contrastButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "High contrast", Exact = true });
        await contrastButton.ClickAsync();
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "contrast");
        await Expect(contrastButton).ToHaveAttributeAsync("aria-pressed", "true");

        var lightButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Light", Exact = true });
        await lightButton.ClickAsync();
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "light");
        await Expect(lightButton).ToHaveAttributeAsync("aria-pressed", "true");

        await _page.ReloadAsync();
        await _page.WaitForInteractiveAsync(".claim-composer__textarea");
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "light");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task MultipleImageEvidence_IsPreviewedAndSent()
    {
        await GoToAppAsync();
        var firstImagePath = Path.Combine(
            Path.GetTempPath(),
            $"claim-front-{Guid.NewGuid():N}.png");
        var secondImagePath = Path.Combine(
            Path.GetTempPath(),
            $"claim-side-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(
            firstImagePath,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        await File.WriteAllBytesAsync(
            secondImagePath,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));

        try
        {
            await ClaimDescription.FillAsync("The front bumper and left fender are damaged.");
            await _page.Locator("input[type=file]")
                .SetInputFilesAsync([firstImagePath, secondImagePath]);
            await Expect(_page.Locator(".claim-composer__attachments img"))
                .ToHaveCountAsync(2);
            await SendButton.ClickAsync();

            await Expect(ClaimDetail("Evidence"))
                .ToHaveTextAsync("2 images", new() { Timeout = 15_000 });
            await Expect(_page.Locator(".claim-findings li"))
                .ToHaveCountAsync(2);
            await Expect(_page.Locator(".claim-photo-gallery figure"))
                .ToHaveCountAsync(2);
        }
        finally
        {
            File.Delete(firstImagePath);
            File.Delete(secondImagePath);
        }

        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task PhotoLimit_AppliesAcrossSubmittedMessages()
    {
        await GoToAppAsync();
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var imagePaths = Enumerable.Range(1, 6)
            .Select(index => Path.Combine(
                Path.GetTempPath(),
                $"claim-quota-{index}-{Guid.NewGuid():N}.png"))
            .ToArray();
        foreach (var imagePath in imagePaths)
        {
            await File.WriteAllBytesAsync(imagePath, imageBytes);
        }

        try
        {
            await ClaimDescription.FillAsync(
                "The front bumper and left fender are damaged.");
            await _page.Locator("input[type=file]")
                .SetInputFilesAsync(imagePaths[..4]);
            await SendButton.ClickAsync();
            await Expect(_page.Locator(".claim-photo-gallery figure"))
                .ToHaveCountAsync(4);
            await _page.GetByRole(
                AriaRole.Button,
                new() { Name = "Cancel claim assessment", Exact = true })
                .ClickAsync();
            await Expect(ClaimDescription).ToBeEnabledAsync();

            await ClaimDescription.FillAsync(
                "The second set shows the same damage from another angle.");
            await _page.Locator("input[type=file]")
                .SetInputFilesAsync(imagePaths[4..]);
            await SendButton.ClickAsync();
            await Expect(_page.Locator(".claim-photo-gallery figure"))
                .ToHaveCountAsync(6);
            await _page.GetByRole(
                AriaRole.Button,
                new() { Name = "Cancel claim assessment", Exact = true })
                .ClickAsync();
            await Expect(ClaimDescription).ToBeEnabledAsync();
            await Expect(_page.Locator("input[type=file]")).ToBeDisabledAsync();
        }
        finally
        {
            foreach (var imagePath in imagePaths)
            {
                File.Delete(imagePath);
            }
        }

        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task VoiceRecording_WithoutTranscriptionDoesNotAttachAudio()
    {
        await _page.AddInitScriptAsync(
            """
            Object.defineProperty(navigator, "mediaDevices", {
                configurable: true,
                value: {
                    getUserMedia: async () => ({
                        getTracks: () => [{ stop() {} }],
                    }),
                },
            });

            class FakeMediaRecorder extends EventTarget {
                static isTypeSupported() {
                    return true;
                }

                constructor(stream, options = {}) {
                    super();
                    this.mimeType = options.mimeType || "audio/webm";
                    this.state = "inactive";
                }

                start() {
                    this.state = "recording";
                }

                stop() {
                    this.state = "inactive";
                    this.dispatchEvent(new Event("stop"));
                }

                requestData() {
                    const data = new Blob([new Uint8Array([1, 2, 3, 4])], {
                        type: this.mimeType,
                    });
                    this.dispatchEvent(new MessageEvent("dataavailable", { data }));
                }
            }

            globalThis.MediaRecorder = FakeMediaRecorder;
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Record voice", Exact = true }).ClickAsync();
        var stopButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop recording", Exact = true });
        await Expect(stopButton).ToBeVisibleAsync();
        await stopButton.ClickAsync();
        await Expect(_page.Locator(".claim-composer__attachments audio"))
            .ToHaveCountAsync(0);
        await Expect(_page.GetByRole(AriaRole.Alert))
            .ToContainTextAsync("Configure Microsoft Foundry");
        await Expect(ClaimDescription).ToBeEnabledAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task CoreComposerInteractions_RemainUsableInOneFlow()
    {
        await AddFakeMediaRecorderAsync();
        await GoToAppAsync();

        foreach (var theme in new[] { "Dark", "High contrast", "Light" })
        {
            await _page.GetByRole(
                AriaRole.Button,
                new() { Name = theme, Exact = true }).ClickAsync();
        }
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "light");

        await Expect(ClaimDescription).ToHaveCSSAsync("text-align", "left");
        await ClaimDescription.FillAsync("The front bumper is damaged.");
        await ClaimDescription.PressAsync("Shift+Enter");
        await ClaimDescription.PressSequentiallyAsync("A second line describes the impact.");
        await Expect(ClaimDescription).ToHaveValueAsync(
            "The front bumper is damaged.\nA second line describes the impact.");
        await Expect(_page.Locator(".sc-ai-turn")).ToHaveCountAsync(0);

        await _page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "front-impact.jpg",
            MimeType = "image/jpeg",
            Buffer = Convert.FromBase64String(TestJpegBase64),
        });
        await Expect(_page.Locator(".claim-composer__attachments img"))
            .ToHaveCountAsync(1);

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Record voice", Exact = true }).ClickAsync();
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop recording", Exact = true }).ClickAsync();
        await Expect(_page.Locator(".claim-composer__attachments audio"))
            .ToHaveCountAsync(0);
        await Expect(ClaimDescription).ToBeEnabledAsync();

        await ClaimDescription.PressAsync("Enter");
        await Expect(ClaimDescription).ToHaveValueAsync(string.Empty);
        var cancel = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Cancel claim assessment", Exact = true });
        await Expect(cancel).ToBeVisibleAsync();
        await cancel.ClickAsync();
        await Expect(ClaimDescription).ToBeEnabledAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Dark", Exact = true }).ClickAsync();
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "dark");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task PhotoDragAndDrop_AttachesImageWithoutBreakingTheCircuit()
    {
        await GoToAppAsync();
        await using var dataTransfer = await _page.EvaluateHandleAsync(
            """
            base64 => {
                const bytes = Uint8Array.from(atob(base64), value => value.charCodeAt(0));
                const transfer = new DataTransfer();
                transfer.items.add(new File([bytes], "dropped-impact.jpg", {
                    type: "image/jpeg",
                }));
                return transfer;
            }
            """,
            TestJpegBase64);
        var composer = _page.Locator(".claim-composer");
        await Expect(composer).ToHaveAttributeAsync("data-sc-ai-drop-zone", "true");
        var eventInit = new Dictionary<string, object?>
        {
            ["dataTransfer"] = dataTransfer,
        };

        await composer.DispatchEventAsync("dragenter", eventInit);
        Assert.IsTrue(await composer.EvaluateAsync<bool>(
            "element => element.classList.contains('sc-ai-drop-zone--active')"));
        await composer.DispatchEventAsync("drop", eventInit);

        await Expect(_page.Locator(".claim-composer__attachments img"))
            .ToHaveCountAsync(1);
        Assert.IsFalse(await composer.EvaluateAsync<bool>(
            "element => element.classList.contains('sc-ai-drop-zone--active')"));
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task LiveVoice_ShowsInterimTextAndSubmitsFinalUtterance()
    {
        await _page.AddInitScriptAsync(
            """
            class ClaimSpeechRecognition extends EventTarget {
                static utteranceIndex = 0;

                start() {
                    globalThis.claimSpeechStartCount++;
                    this.dispatchEvent(new Event("start"));
                    const utteranceIndex = ClaimSpeechRecognition.utteranceIndex++;
                    if (utteranceIndex > 1) {
                        return;
                    }

                    if (utteranceIndex === 0) {
                        setTimeout(() => this.emit(
                            "My 2022 sedan has a cracked front",
                            false), 100);
                        setTimeout(() => this.emit(
                            "My 2022 sedan has a cracked front bumper after a parking collision.",
                            true), 750);
                    } else {
                        setTimeout(() => this.emit(
                            "What should I do",
                            false), 100);
                        setTimeout(() => this.emit(
                            "What should I do next?",
                            true), 750);
                    }
                }

                stop() {
                    this.dispatchEvent(new Event("end"));
                }

                abort() {
                    this.dispatchEvent(new Event("end"));
                }

                emit(transcript, isFinal) {
                    const result = [{ transcript }];
                    result.isFinal = isFinal;
                    const event = new Event("result");
                    event.resultIndex = 0;
                    event.results = [result];
                    this.dispatchEvent(event);
                }
            }

            globalThis.claimSpeechStartCount = 0;
            globalThis.SpeechRecognition = ClaimSpeechRecognition;
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Start live voice", Exact = true }).ClickAsync();
        await Expect(ClaimDescription).ToBeDisabledAsync();
        await Expect(ClaimDescription).ToHaveValueAsync(string.Empty);
        await Expect(_page.Locator(".claim-live-transcript"))
            .ToContainTextAsync("My 2022 sedan has a cracked front");
        await Expect(ClaimDescription).ToBeDisabledAsync();
        await Expect(_page.GetByText(
            "My 2022 sedan has a cracked front bumper after a parking collision.",
            new() { Exact = true }).First)
            .ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(ClaimDetail("Accident"))
            .ToContainTextAsync("2022 sedan", new() { Timeout = 120_000 });
        await Expect(_page.Locator(".claim-live-transcript"))
            .ToHaveCountAsync(0);

        var approveAssessment = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve assessment", Exact = true });
        var composerMetadata =
            await _page.Locator(".claim-composer__meta").InnerTextAsync();
        if (composerMetadata.Contains("Local simulator", StringComparison.Ordinal))
        {
            await Expect(approveAssessment)
                .ToBeVisibleAsync(new() { Timeout = 30_000 });
            await approveAssessment.ClickAsync();
        }

        await _page.WaitForFunctionAsync(
            "() => globalThis.claimSpeechStartCount >= 2",
            null,
            new() { Timeout = 120_000 });
        await Expect(_page.GetByText(
            "What should I do next?",
            new() { Exact = true }).First)
            .ToBeVisibleAsync(new() { Timeout = 120_000 });
        await _page.WaitForFunctionAsync(
            "() => globalThis.claimSpeechStartCount >= 3",
            null,
            new() { Timeout = 120_000 });
        var stopLiveVoice = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop live voice", Exact = true });
        await Expect(stopLiveVoice).ToBeVisibleAsync();
        await Expect(ClaimDescription).ToBeDisabledAsync();
        await stopLiveVoice.ClickAsync();
        await Expect(_page.Locator(".sc-ai-input__live-speech"))
            .ToHaveAttributeAsync("aria-pressed", "false");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task RejectedMediaInput_ShowsInlineErrorsAndKeepsTheCircuitAlive()
    {
        await _page.AddInitScriptAsync(
            """
            Object.defineProperty(navigator, "mediaDevices", {
                configurable: true,
                value: {
                    getUserMedia: async () => {
                        throw new DOMException("Permission denied", "NotAllowedError");
                    },
                },
            });
            """);
        await GoToAppAsync();

        await _page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "unsupported-image.heic",
            MimeType = "image/heic",
            Buffer = [1, 2, 3, 4],
        });
        await Expect(_page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Record voice", Exact = true }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Alert))
            .ToContainTextAsync("Microphone access was not available");

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "High contrast", Exact = true }).ClickAsync();
        await Expect(AppRoot).ToHaveAttributeAsync("data-theme", "contrast");
        await Expect(ClaimDescription).ToBeEnabledAsync();
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task FoundryVoiceEvidence_IsCapturedAndTranscribed()
    {
        var audioPath = Environment.GetEnvironmentVariable("CLAIM_TEST_AUDIO_PATH");
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")) ||
            string.IsNullOrWhiteSpace(audioPath) ||
            !File.Exists(audioPath))
        {
            Assert.Inconclusive("Foundry transcription and test audio are not configured.");
        }

        var audioBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(audioPath));
        var serializedAudio = System.Text.Json.JsonSerializer.Serialize(audioBase64);
        var serializedMediaType = System.Text.Json.JsonSerializer.Serialize(
            Environment.GetEnvironmentVariable("CLAIM_TEST_AUDIO_MEDIA_TYPE") ??
            "audio/wav");
        await _page.AddInitScriptAsync(
            $$"""
            const claimTestAudio = {{serializedAudio}};
            const claimTestAudioMediaType = {{serializedMediaType}};
            Object.defineProperty(navigator, "mediaDevices", {
                configurable: true,
                value: {
                    getUserMedia: async () => ({
                        getTracks: () => [{ stop() {} }],
                    }),
                },
            });

            class FoundryAudioRecorder extends EventTarget {
                static isTypeSupported() {
                    return false;
                }

                constructor() {
                    super();
                    this.mimeType = claimTestAudioMediaType;
                    this.state = "inactive";
                }

                start() {
                    this.state = "recording";
                }

                requestData() {
                    const binary = atob(claimTestAudio);
                    const bytes = new Uint8Array(binary.length);
                    for (let index = 0; index < binary.length; index++) {
                        bytes[index] = binary.charCodeAt(index);
                    }
                    const data = new Blob([bytes], { type: this.mimeType });
                    this.dispatchEvent(new MessageEvent("dataavailable", { data }));
                }

                stop() {
                    this.state = "inactive";
                    this.dispatchEvent(new Event("stop"));
                }
            }

            class RecordedSpeechRecognition extends EventTarget {
                start() {
                    this.dispatchEvent(new Event("start"));
                    setTimeout(() => this.emit(
                        "The front bumper is cracked",
                        false), 100);
                    setTimeout(() => this.emit(
                        "The front bumper is cracked and the left headlight is broken.",
                        true), 600);
                }

                stop() {
                    this.dispatchEvent(new Event("end"));
                }

                abort() {
                    this.dispatchEvent(new Event("end"));
                }

                emit(transcript, isFinal) {
                    const result = [{ transcript }];
                    result.isFinal = isFinal;
                    const event = new Event("result");
                    event.resultIndex = 0;
                    event.results = [result];
                    this.dispatchEvent(event);
                }
            }

            globalThis.MediaRecorder = FoundryAudioRecorder;
            globalThis.SpeechRecognition = RecordedSpeechRecognition;
            """);
        await GoToAppAsync();

        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Record voice", Exact = true }).ClickAsync();
        await Expect(ClaimDescription)
            .ToHaveValueAsync(
                new System.Text.RegularExpressions.Regex(
                    "front bumper",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Stop recording", Exact = true }).ClickAsync();
        await Expect(_page.Locator(".claim-composer__attachments audio"))
            .ToHaveCountAsync(0);
        await Expect(ClaimDescription)
            .ToHaveValueAsync(
                new System.Text.RegularExpressions.Regex(
                    "front bumper",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                new() { Timeout = 120_000 });

        await SendButton.ClickAsync();
        await Expect(ClaimDetail("Accident"))
            .ToContainTextAsync("front bumper", new() { Timeout = 120_000 });
        await Expect(_page.GetByText(
            new System.Text.RegularExpressions.Regex(
                @"recording-\d{8}-\d{6}\.webm",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
            .ToHaveCountAsync(0);
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task Rejection_RequiresAReasonAndCompletes()
    {
        await GoToAppAsync();
        await _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Side impact", Exact = true }).ClickAsync();
        await SendButton.ClickAsync();

        var rejectButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Reject assessment", Exact = true });
        await Expect(rejectButton).ToBeVisibleAsync(new() { Timeout = 20_000 });
        await rejectButton.ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Alert))
            .ToHaveTextAsync("Enter a reason before rejecting the assessment.");

        await _page.GetByRole(
            AriaRole.Textbox,
            new() { Name = "Reason if rejecting", Exact = true })
            .FillAsync("The rear door is also damaged.");
        await rejectButton.ClickAsync();

        await Expect(ClaimDetail("Decision"))
            .ToHaveTextAsync("Rejected", new() { Timeout = 15_000 });
        await Expect(ClaimDescription).ToBeEnabledAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task Cancel_StopsTheAssessmentAndAllowsAnotherMessage()
    {
        await GoToAppAsync();
        await ClaimDescription.FillAsync("A vehicle hit the rear bumper and left door.");
        await SendButton.ClickAsync();

        var cancelButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Cancel claim assessment", Exact = true });
        await Expect(cancelButton).ToBeVisibleAsync();
        await cancelButton.ClickAsync();

        await Expect(ClaimDescription).ToBeEnabledAsync();
        await Expect(_page.Locator(".claim-chat-header").GetByRole(AriaRole.Status))
            .ToContainTextAsync("Ready");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task Keyboard_SubmitsAndEscapeStopsTheAssessment()
    {
        await GoToAppAsync();
        await ClaimDescription.FillAsync(
            "A vehicle hit the rear bumper and left door.");
        await ClaimDescription.PressAsync("Enter");

        await Expect(_page.GetByRole(
            AriaRole.Button,
            new() { Name = "Cancel claim assessment", Exact = true }))
            .ToBeVisibleAsync();
        await _page.Keyboard.PressAsync("Escape");

        await Expect(ClaimDescription).ToBeEnabledAsync();
        await Expect(ClaimDescription).ToHaveValueAsync(string.Empty);
        await Expect(_page.Locator(".claim-chat-header").GetByRole(AriaRole.Status))
            .ToContainTextAsync("Ready");
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task Retry_RecoversFromATransientAssessmentError()
    {
        await GoToAppAsync();
        await ClaimDescription.FillAsync("Trigger an error while assessing the front bumper.");
        await SendButton.ClickAsync();

        var retryButton = _page.GetByRole(
            AriaRole.Button,
            new() { Name = "Retry", Exact = true });
        await Expect(retryButton).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await retryButton.ClickAsync();

        await Expect(_page.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve assessment", Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 20_000 });
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task DesktopEmptyState_KeepsIntakeActionsTogether()
    {
        await _page.SetViewportSizeAsync(1600, 1200);
        await GoToAppAsync();

        var workspace = await _page.Locator(".claim-workspace").BoundingBoxAsync();
        var chat = await _page.Locator(".claim-chat").BoundingBoxAsync();
        var summary = await _page.Locator(".claim-summary").BoundingBoxAsync();
        var welcomeHeading = await _page.GetByRole(
            AriaRole.Heading,
            new() { Name = "Start with what happened.", Exact = true })
            .BoundingBoxAsync();
        var suggestions = await _page.Locator(".claim-composer__suggestions")
            .BoundingBoxAsync();
        var composer = await _page.Locator(".claim-composer .sc-ai-input")
            .BoundingBoxAsync();

        Assert.IsNotNull(workspace);
        Assert.IsNotNull(chat);
        Assert.IsNotNull(summary);
        Assert.IsNotNull(welcomeHeading);
        Assert.IsNotNull(suggestions);
        Assert.IsNotNull(composer);
        Assert.IsTrue(workspace.Width <= 1408);
        Assert.IsTrue(workspace.Height <= 832);
        Assert.IsTrue(summary.Height <= chat.Height);
        Assert.IsTrue(composer.Width <= 896);
        Assert.IsTrue(Math.Abs(welcomeHeading.X - composer.X) < 48);
        Assert.IsTrue(suggestions.Y - (welcomeHeading.Y + welcomeHeading.Height) < 180);
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task MobileLayout_StacksWithoutHorizontalOverflow()
    {
        await _page.SetViewportSizeAsync(390, 844);
        await GoToAppAsync();

        var columns = await _page.Locator(".claim-workspace").EvaluateAsync<string>(
            "element => getComputedStyle(element).gridTemplateColumns");
        Assert.HasCount(1, columns.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var chat = await _page.Locator(".claim-chat").BoundingBoxAsync();
        var summary = await _page.Locator(".claim-summary").BoundingBoxAsync();
        Assert.IsNotNull(chat);
        Assert.IsNotNull(summary);
        Assert.IsTrue(chat.Y < summary.Y);

        var hasHorizontalOverflow = await _page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > window.innerWidth");
        Assert.IsFalse(hasHorizontalOverflow);
        await Expect(ClaimDescription).ToBeVisibleAsync();
        AssertNoBrowserErrors();
    }

    [TestMethod]
    public async Task FoundryWorkflow_ReturnsGroundedRepairIntelligence()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")))
        {
            Assert.Inconclusive("The Foundry claim endpoint is not configured.");
        }

        await GoToAppAsync();
        await Expect(_page.Locator(".claim-composer__meta"))
            .ToContainTextAsync("Model · gpt-5-mini");

        var imagePath = Path.Combine(
            Path.GetTempPath(),
            $"claim-foundry-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(
            imagePath,
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));

        try
        {
            await ClaimDescription.FillAsync(
                "My 2022 Toyota Camry SE was hit in Seattle, Washington. " +
                "The front bumper is cracked and the left headlight is broken.");
            await _page.Locator("input[type=file]").SetInputFilesAsync(imagePath);
            await Expect(_page.Locator(".claim-composer__attachments img"))
                .ToHaveCountAsync(1);
            await SendButton.ClickAsync();

            await Expect(ClaimDetail("Evidence"))
                .ToHaveTextAsync("1 image", new() { Timeout = 120_000 });
            await Expect(_page.Locator(".claim-estimate"))
                .ToBeVisibleAsync(new() { Timeout = 180_000 });
            Assert.IsGreaterThan(
                0,
                await _page.Locator(".claim-parts li").CountAsync());
            Assert.IsGreaterThan(
                0,
                await _page.Locator(".claim-sources a").CountAsync());
            await Expect(_page.GetByRole(
                AriaRole.Button,
                new() { Name = "Approve assessment", Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 60_000 });
        }
        finally
        {
            File.Delete(imagePath);
        }

        AssertNoBrowserErrors();
    }

    private ILocator AppRoot => _page.Locator(".claim-app");

    private ILocator ClaimDescription => _page.GetByRole(
        AriaRole.Textbox,
        new() { Name = "Claim description", Exact = true });

    private ILocator SendButton => _page.GetByRole(
        AriaRole.Button,
        new() { Name = "Send", Exact = true });

    private ILocator ClaimDetail(string name)
        => _page.Locator(".claim-details div")
            .Filter(new() { Has = _page.Locator("dt", new() { HasText = name }) })
            .Locator("dd");

    private async Task GoToAppAsync()
    {
        await _page.GotoAsync(_testUrl);
        await _page.WaitForInteractiveAsync(".claim-composer__textarea");
    }

    private async Task AddFakeMediaRecorderAsync()
    {
        await _page.AddInitScriptAsync(
            """
            Object.defineProperty(navigator, "mediaDevices", {
                configurable: true,
                value: {
                    getUserMedia: async () => ({
                        getTracks: () => [{ stop() {} }],
                    }),
                },
            });

            class StableMediaRecorder extends EventTarget {
                static isTypeSupported() {
                    return true;
                }

                constructor(stream, options = {}) {
                    super();
                    this.mimeType = options.mimeType || "audio/webm";
                    this.state = "inactive";
                }

                start() {
                    this.state = "recording";
                }

                stop() {
                    this.state = "inactive";
                    this.dispatchEvent(new Event("stop"));
                }

                requestData() {
                    const data = new Blob([new Uint8Array([1, 2, 3, 4])], {
                        type: this.mimeType,
                    });
                    this.dispatchEvent(new MessageEvent("dataavailable", { data }));
                }
            }

            globalThis.MediaRecorder = StableMediaRecorder;
            """);
    }

    private void AssertNoBrowserErrors()
    {
        Assert.AreEqual(
            0,
            _pageErrors.Count,
            string.Join(Environment.NewLine, _pageErrors));
    }
}
