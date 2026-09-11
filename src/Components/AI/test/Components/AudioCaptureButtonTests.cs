// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class AudioCaptureButtonTests
{
    [Fact]
    public async Task AutoSubmit_SubmitsBackendTranscriptAfterRecordingStops()
    {
        var submittedMessages = new List<ChatMessage>();
        var (cut, input, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("verified transcript"),
            autoSubmit: true,
            onSubmitted: message => submittedMessages.Add(message));
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));

        var message = Assert.Single(submittedMessages);
        Assert.Equal("verified transcript", message.Text);
        Assert.Equal(string.Empty, input.Text);
        Assert.Equal(1, recorder.StopCount);
        Assert.False(input.IsComposing);
    }

    [Fact]
    public async Task BrowserRecognition_AutoSubmitsAndResumesWithoutRecording()
    {
        var submittedMessages = new List<ChatMessage>();
        var speechRecognizer = new TestSpeechRecognizer();
        var (cut, input, recorder) = RenderAudioCapture(
            transcribe: null,
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognition,
            autoSubmit: true,
            continuousListening: true,
            onSubmitted: message => submittedMessages.Add(message),
            speechRecognizer: speechRecognizer);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await speechRecognizer.EmitResultAsync("browser transcript", string.Empty);

        var message = Assert.Single(submittedMessages);
        Assert.Equal("browser transcript", message.Text);
        Assert.Equal(string.Empty, input.Text);
        Assert.Equal(0, recorder.StartCount);
        Assert.Equal(2, speechRecognizer.StartCount);
        Assert.Equal(1, speechRecognizer.StopCount);
        Assert.True(input.IsComposing);
    }

    [Fact]
    public async Task BrowserRecognition_PermissionRevokedStopsListening()
    {
        var speechRecognizer = new TestSpeechRecognizer();
        var (cut, input, _) = RenderAudioCapture(
            transcribe: null,
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognition,
            autoSubmit: true,
            continuousListening: true,
            speechRecognizer: speechRecognizer);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await speechRecognizer.EmitResultAsync(string.Empty, "unfinished");
        await speechRecognizer.EmitErrorAsync("not-allowed", isFatal: true);

        Assert.False(input.IsComposing);
        Assert.Equal("unfinished", input.Text);
        Assert.Null(input.ErrorMessage);
        Assert.Null(input.StatusMessage);
        Assert.Equal(1, speechRecognizer.StartCount);
        Assert.Equal(MicrophonePermissionStatus.Denied, input.MicrophonePermissionStatus);
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Contains("Microphone access is blocked.", cut.GetHtml());
        Assert.DoesNotContain("Microphone unavailable", cut.GetHtml());
        Assert.Equal("false", GetAttribute(button, "aria-pressed"));
    }

    [Fact]
    public async Task Recording_PermissionRevokedStopsRecording()
    {
        var (cut, input, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await recorder.EmitErrorAsync("permission-revoked");

        Assert.False(input.IsComposing);
        Assert.Null(input.ErrorMessage);
        Assert.Null(input.StatusMessage);
        Assert.Equal(MicrophonePermissionStatus.Denied, input.MicrophonePermissionStatus);
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Contains("Microphone access is blocked.", cut.GetHtml());
        Assert.DoesNotContain("Microphone unavailable", cut.GetHtml());
        Assert.Equal("false", GetAttribute(button, "aria-pressed"));
    }

    [Fact]
    public async Task Recording_StartFlowsCancellationTokenToInterop()
    {
        var (cut, _, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.True(recorder.StartToken.CanBeCanceled);
    }

    [Fact]
    public async Task Recording_MissingMimeTypeReportsError()
    {
        var recorder = new TestAudioRecorder
        {
            MimeType = string.Empty,
        };
        var (cut, input, _) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"),
            recorder: recorder);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.Equal(
            "The browser did not provide the recorded audio MIME type.",
            input.ErrorMessage);
    }

    [Fact]
    public async Task Recording_InteropFailureIncludesExceptionDetails()
    {
        var recorder = new TestAudioRecorder
        {
            StartException = new JSException("Permission denied by browser policy."),
        };
        var (cut, input, _) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"),
            recorder: recorder);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.Equal(
            "Audio recording could not be initialized. Permission denied by browser policy.",
            input.ErrorMessage);
    }

    [Fact]
    public async Task Recording_PermissionDeniedClearsPendingStatus()
    {
        var (cut, input, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var button = cut.FindComponent<AudioCaptureButton>();
        recorder.FailStart();

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.False(input.IsComposing);
        Assert.Null(input.StatusMessage);
        Assert.Null(input.ErrorMessage);
        Assert.Equal(MicrophonePermissionStatus.Denied, input.MicrophonePermissionStatus);
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Contains("Microphone access is blocked.", cut.GetHtml());
        Assert.DoesNotContain("Microphone unavailable", cut.GetHtml());
        Assert.Equal("false", GetAttribute(button, "aria-busy"));
        Assert.Equal("false", GetAttribute(button, "aria-pressed"));
    }

    [Fact]
    public async Task Recording_NoMicrophoneShowsUnavailableNotice()
    {
        var (cut, input, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var button = cut.FindComponent<AudioCaptureButton>();
        recorder.FailStart("unavailable");

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.False(input.IsComposing);
        Assert.Equal(
            MicrophonePermissionStatus.Unavailable,
            input.MicrophonePermissionStatus);
        Assert.Contains("No microphone is available.", cut.GetHtml());
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Equal("false", GetAttribute(button, "aria-busy"));
    }

    [Fact]
    public async Task DisposingOneControl_DoesNotClearAnotherControlsPermissionFeedback()
    {
        var (cut, input, _) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var firstButton = cut.FindComponent<AudioCaptureButton>();
        var secondButton = new AudioCaptureButton
        {
            Context = input,
        };

        await cut.InvokeAsync(() => input.SetMicrophonePermissionStatus(
            secondButton,
            MicrophonePermissionStatus.Denied));
        await cut.InvokeAsync(() => firstButton.Instance.DisposeAsync().AsTask());

        Assert.Equal(
            MicrophonePermissionStatus.Denied,
            input.MicrophonePermissionStatus);
        Assert.Contains("Microphone access is blocked.", cut.GetHtml());
    }

    [Fact]
    public async Task Recording_UnknownStartFailureShowsInitializationError()
    {
        var (cut, input, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var button = cut.FindComponent<AudioCaptureButton>();
        recorder.FailStart("unexpected");

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.False(input.IsComposing);
        Assert.Equal(
            MicrophonePermissionStatus.None,
            input.MicrophonePermissionStatus);
        Assert.Equal(
            "Audio recording could not be initialized.",
            input.ErrorMessage);
        Assert.DoesNotContain(
            "sc-ai-input__microphone-permission",
            cut.GetHtml());
    }

    [Fact]
    public async Task Recording_PermissionPendingDescribesAndDisablesControl()
    {
        var (cut, input, recorder) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"));
        var button = cut.FindComponent<AudioCaptureButton>();
        var permissionRequested = recorder.PauseStart();

        var clickTask = cut.InvokeAsync(() => ClickAsync(button));
        await permissionRequested;

        Assert.True(input.IsComposing);
        Assert.Null(input.StatusMessage);
        Assert.Equal(
            MicrophonePermissionStatus.Requesting,
            input.MicrophonePermissionStatus);
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Equal(true, GetAttribute(button, "disabled"));
        Assert.Equal("true", GetAttribute(button, "aria-busy"));
        Assert.Equal("false", GetAttribute(button, "aria-pressed"));
        Assert.Contains("Waiting for microphone permission...", cut.GetHtml());
        Assert.Contains(">Record audio</button>", cut.GetHtml());

        recorder.CompleteStart();
        await clickTask;

        Assert.Equal("Recording audio.", input.StatusMessage);
        Assert.Equal(MicrophonePermissionStatus.None, input.MicrophonePermissionStatus);
        Assert.Equal("Stop recording", GetAttribute(button, "aria-label"));
        Assert.Null(GetAttribute(button, "disabled"));
        Assert.Equal("false", GetAttribute(button, "aria-busy"));
        Assert.Equal("true", GetAttribute(button, "aria-pressed"));
    }

    [Fact]
    public async Task BrowserRecognition_PermissionPendingUntilRecognitionStarts()
    {
        var speechRecognizer = new TestSpeechRecognizer();
        var (cut, input, _) = RenderAudioCapture(
            transcribe: null,
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognition,
            speechRecognizer: speechRecognizer);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.True(input.IsComposing);
        Assert.Null(input.StatusMessage);
        Assert.Equal(
            MicrophonePermissionStatus.Requesting,
            input.MicrophonePermissionStatus);
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Equal(true, GetAttribute(button, "disabled"));
        Assert.Equal("true", GetAttribute(button, "aria-busy"));
        Assert.Equal("false", GetAttribute(button, "aria-pressed"));
        Assert.Contains("Waiting for microphone permission...", cut.GetHtml());
        Assert.Contains(">Record audio</button>", cut.GetHtml());

        await speechRecognizer.EmitStartedAsync();

        Assert.Equal("Listening for your next instruction.", input.StatusMessage);
        Assert.Equal(MicrophonePermissionStatus.None, input.MicrophonePermissionStatus);
        Assert.Equal("Stop recording", GetAttribute(button, "aria-label"));
        Assert.Null(GetAttribute(button, "disabled"));
        Assert.Equal("false", GetAttribute(button, "aria-busy"));
        Assert.Equal("true", GetAttribute(button, "aria-pressed"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrowserRecognition_InitializationFailureShowsComposerError(
        bool failsDuringCreation)
    {
        var speechRecognizer = new TestSpeechRecognizer();
        if (failsDuringCreation)
        {
            speechRecognizer.FailCreation();
        }
        else
        {
            speechRecognizer.FailStart();
        }

        var (cut, input, _) = RenderAudioCapture(
            transcribe: null,
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognition,
            speechRecognizer: speechRecognizer);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.False(input.IsComposing);
        Assert.Equal(
            MicrophonePermissionStatus.None,
            input.MicrophonePermissionStatus);
        Assert.Equal(
            failsDuringCreation
                ? "Voice input could not be initialized. Speech recognition could not be created."
                : "Voice input could not be initialized. Speech recognition failed to start.",
            input.ErrorMessage);
        Assert.DoesNotContain(
            "sc-ai-input__microphone-permission",
            cut.GetHtml());
    }

    [Fact]
    public void BrowserRecognition_UnsupportedDoesNotRenderControl()
    {
        var (cut, input, _) = RenderAudioCapture(
            transcribe: null,
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognition,
            speechRecognitionSupported: false);

        Assert.DoesNotContain("sc-ai-input__audio", cut.GetHtml());
        Assert.Null(input.ErrorMessage);
    }

    [Fact]
    public void CustomAriaLabels_AreIndependentFromVisibleContent()
    {
        var (cut, _, _) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"),
            startAriaLabel: "Start voice input");
        var button = cut.FindComponent<AudioCaptureButton>();

        Assert.Equal("Start voice input", GetAttribute(button, "aria-label"));
        Assert.Contains("Record audio", button.GetHtml());
    }

    [Fact]
    public async Task BrowserRecognition_InterimAndFinalResultsUpdateTextWithoutClearingIt()
    {
        var speechRecognizer = new TestSpeechRecognizer();
        var transcriptChanges = new List<string>();
        var (cut, input, _) = RenderAudioCapture(
            transcribe: null,
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognition,
            speechRecognizer: speechRecognizer,
            onInterimTranscript: transcriptChanges.Add);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await speechRecognizer.EmitResultAsync(string.Empty, "hello wor");
        Assert.Equal("hello wor", input.Text);

        await speechRecognizer.EmitResultAsync("hello world", string.Empty);

        Assert.Equal("hello world", input.Text);
        Assert.Equal(["hello wor", "hello world"], transcriptChanges);
    }

    [Fact]
    public async Task CombinedRecognition_CachesBrowserSupportCheck()
    {
        var speechRecognizer = new TestSpeechRecognizer();
        var module = new TestAudioModule(
            new TestAudioRecorder(),
            speechRecognizer,
            speechRecognitionSupported: true);
        var (cut, _, _) = RenderAudioCapture(
            (_, _) => ValueTask.FromResult<string?>("transcript"),
            recognitionMode: SpeechRecognitionMode.BrowserSpeechRecognitionWithBackendTranscription,
            module: module);
        var button = cut.FindComponent<AudioCaptureButton>();

        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));

        Assert.Equal(1, module.SpeechRecognitionSupportCheckCount);
    }

    [Fact]
    public async Task ClickWhileTranscribing_CancelsOperationWithoutStoppingRecorderAgain()
    {
        var transcriptionStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken transcriptionToken = default;
        async ValueTask<string?> TranscribeAsync(
            DataContent _,
            CancellationToken cancellationToken)
        {
            transcriptionToken = cancellationToken;
            transcriptionStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }

        var (cut, input, recorder) = RenderAudioCapture(TranscribeAsync);
        var button = cut.FindComponent<AudioCaptureButton>();
        await cut.InvokeAsync(() => ClickAsync(button));

        var stopTask = cut.InvokeAsync(() => ClickAsync(button));
        await transcriptionStarted.Task;
        Assert.Equal("Stop recording", GetAttribute(button, "aria-label"));
        Assert.Equal("true", GetAttribute(button, "aria-pressed"));

        await cut.InvokeAsync(() => ClickAsync(button));
        await stopTask;

        Assert.True(transcriptionToken.CanBeCanceled);
        Assert.True(transcriptionToken.IsCancellationRequested);
        Assert.True(recorder.StreamReference.OpenToken.CanBeCanceled);
        Assert.Equal(1, recorder.StopCount);
        Assert.False(input.IsComposing);
        Assert.Null(input.ErrorMessage);
        Assert.Equal("Audio transcription canceled.", input.StatusMessage);
        Assert.Equal("Record audio", GetAttribute(button, "aria-label"));
        Assert.Equal("false", GetAttribute(button, "aria-pressed"));
    }

    [Fact]
    public async Task SupersededTranscription_DoesNotOverwriteNewerResult()
    {
        var firstTranscriptionStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstTranscription = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var transcriptionCount = 0;
        async ValueTask<string?> TranscribeAsync(
            DataContent _,
            CancellationToken cancellationToken)
        {
            if (++transcriptionCount == 1)
            {
                firstTranscriptionStarted.SetResult();
                await releaseFirstTranscription.Task;
                return "stale transcript";
            }

            cancellationToken.ThrowIfCancellationRequested();
            return "new transcript";
        }

        var (cut, input, recorder) = RenderAudioCapture(TranscribeAsync);
        var button = cut.FindComponent<AudioCaptureButton>();
        await cut.InvokeAsync(() => ClickAsync(button));
        var firstStopTask = cut.InvokeAsync(() => ClickAsync(button));
        await firstTranscriptionStarted.Task;

        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));
        await cut.InvokeAsync(() => ClickAsync(button));
        releaseFirstTranscription.SetResult();
        await firstStopTask;

        Assert.Equal(2, recorder.StopCount);
        Assert.Equal("new transcript", input.Text);
        Assert.False(input.IsComposing);
        Assert.Null(input.ErrorMessage);
    }

    private static (
        RenderedComponent<AgentBoundary> Component,
        MessageInputContext Input,
        TestAudioRecorder Recorder) RenderAudioCapture(
            Func<DataContent, CancellationToken, ValueTask<string?>>? transcribe,
            SpeechRecognitionMode recognitionMode = SpeechRecognitionMode.BackendTranscription,
            bool autoSubmit = false,
            bool continuousListening = false,
            Action<ChatMessage>? onSubmitted = null,
            TestSpeechRecognizer? speechRecognizer = null,
            bool speechRecognitionSupported = true,
            Action<string>? onInterimTranscript = null,
            string? startAriaLabel = null,
            TestAudioRecorder? recorder = null,
            TestAudioModule? module = null)
    {
        recorder ??= new TestAudioRecorder();
        module ??= new TestAudioModule(
            recorder,
            speechRecognizer ?? new TestSpeechRecognizer(),
            speechRecognitionSupported);
        var services = new TestServiceProvider();
        services.AddService<IJSRuntime>(new TestJSRuntime(module));
        var renderer = new TestRenderer(services);
        MessageInputContext? input = null;
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((_, _, cancellationToken) =>
            ResponseEmitters.EmitTextResponse("Done", cancellationToken));
        var agent = new UIAgent(client);
        var cut = renderer.RenderComponent<AgentBoundary>(parameters =>
        {
            parameters[nameof(AgentBoundary.Agent)] = agent;
            parameters[nameof(AgentBoundary.ChildContent)] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MessageInput>(0);
                builder.AddComponentParameter(
                    1,
                    nameof(MessageInput.OnSubmitted),
                    EventCallback.Factory.Create<ChatMessage>(
                        receiver: new object(),
                        message => onSubmitted?.Invoke(message)));
                builder.AddComponentParameter(
                    2,
                    nameof(MessageInput.TopContent),
                    (RenderFragment<MessageInputContext>)(context => childBuilder =>
                    {
                        input = context;
                    }));
                builder.AddComponentParameter(
                    3,
                    nameof(MessageInput.LeadingActions),
                    (RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<AudioCaptureButton>(0);
                        childBuilder.AddComponentParameter(
                            1,
                            nameof(AudioCaptureButton.AttachRecording),
                            false);
                        childBuilder.AddComponentParameter(
                            2,
                            nameof(AudioCaptureButton.Transcribe),
                            transcribe);
                        childBuilder.AddComponentParameter(
                            3,
                            nameof(AudioCaptureButton.RecognitionMode),
                            recognitionMode);
                        childBuilder.AddComponentParameter(
                            4,
                            nameof(AudioCaptureButton.AutoSubmit),
                            autoSubmit);
                        childBuilder.AddComponentParameter(
                            5,
                            nameof(AudioCaptureButton.ContinuousListening),
                            continuousListening);
                        childBuilder.AddComponentParameter(
                            6,
                            nameof(AudioCaptureButton.OnInterimTranscript),
                            EventCallback.Factory.Create<string>(
                                receiver: new object(),
                                transcript => onInterimTranscript?.Invoke(transcript)));
                        childBuilder.AddComponentParameter(
                            7,
                            nameof(AudioCaptureButton.StartAriaLabel),
                            startAriaLabel);
                        childBuilder.CloseComponent();
                    }));
                builder.CloseComponent();
            });
        });

        return (cut, input!, recorder);
    }

    private static Task ClickAsync(RenderedComponent<AudioCaptureButton> button)
    {
        var frames = button.GetFrames();
        var callback = frames.Array
            .Take(frames.Count)
            .Single(frame =>
                frame.FrameType == RenderTreeFrameType.Attribute &&
                frame.AttributeName == "onclick")
            .AttributeValue;
        return callback switch
        {
            Func<Task> handler => handler(),
            EventCallback eventCallback => eventCallback.InvokeAsync(),
            _ => throw new InvalidOperationException(
                $"Unexpected click callback type {callback?.GetType().FullName}."),
        };
    }

    private static object? GetAttribute(
        RenderedComponent<AudioCaptureButton> button,
        string attributeName)
    {
        var frames = button.GetFrames();
        return frames.Array
            .Take(frames.Count)
            .SingleOrDefault(frame =>
                frame.FrameType == RenderTreeFrameType.Attribute &&
                frame.AttributeName == attributeName)
            .AttributeValue;
    }

    private sealed class TestJSRuntime(TestAudioModule module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => identifier == "import"
                ? ValueTask.FromResult((TValue)(object)module)
                : ValueTask.FromResult(default(TValue)!);
    }

    private sealed class TestAudioModule(
        TestAudioRecorder recorder,
        TestSpeechRecognizer speechRecognizer,
        bool speechRecognitionSupported) : IJSObjectReference
    {
        internal int SpeechRecognitionSupportCheckCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            return identifier switch
            {
                "isAudioCaptureSupported" =>
                    ValueTask.FromResult((TValue)(object)true),
                "isLiveSpeechRecognitionSupported" =>
                    GetSpeechRecognitionSupport<TValue>(),
                "createAudioRecorder" =>
                    CreateAudioRecorder<TValue>(args),
                "createLiveSpeechRecognizer" =>
                    CreateSpeechRecognizer<TValue>(args),
                _ => ValueTask.FromResult(default(TValue)!),
            };
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private ValueTask<TValue> GetSpeechRecognitionSupport<TValue>()
        {
            SpeechRecognitionSupportCheckCount++;
            return ValueTask.FromResult((TValue)(object)speechRecognitionSupported);
        }

        private ValueTask<TValue> CreateSpeechRecognizer<TValue>(object?[]? args)
        {
            if (speechRecognizer.CreationFails)
            {
                throw new JSException("Speech recognition could not be created.");
            }

            speechRecognizer.SetCallbacks(args![0]!);
            return ValueTask.FromResult((TValue)(object)speechRecognizer);
        }

        private ValueTask<TValue> CreateAudioRecorder<TValue>(object?[]? args)
        {
            recorder.SetCallbacks(args![1]!);
            return ValueTask.FromResult((TValue)(object)recorder);
        }
    }

    private sealed class TestAudioRecorder : IJSObjectReference
    {
        private object? _callbacks;
        private string? _startFailure;
        private TaskCompletionSource? _startCompletion;
        private TaskCompletionSource? _startRequested;

        internal TestStreamReference StreamReference { get; } = new();

        internal int StopCount { get; private set; }

        internal int StartCount { get; private set; }

        internal CancellationToken StartToken { get; private set; }

        internal string MimeType { get; init; } = "audio/webm";

        internal JSException? StartException { get; init; }

        internal Task PauseStart()
        {
            _startCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _startRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return _startRequested.Task;
        }

        internal void CompleteStart()
        {
            _startCompletion?.SetResult();
        }

        internal void FailStart(string failure = "denied")
        {
            _startFailure = failure;
        }

        internal void SetCallbacks(object callbacks)
        {
            _callbacks = callbacks;
        }

        internal Task EmitErrorAsync(string error)
        {
            return InvokeCallbackAsync("OnRecordingErrorAsync", error);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "start")
            {
                return StartAsync<TValue>(cancellationToken);
            }

            if (identifier != "stop")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            StopCount++;
            var result = Activator.CreateInstance(typeof(TValue), nonPublic: true)!;
            typeof(TValue).GetProperty("StreamReference")!
                .SetValue(result, StreamReference);
            typeof(TValue).GetProperty("MimeType")!
                .SetValue(result, MimeType);
            typeof(TValue).GetProperty("Size")!
                .SetValue(result, StreamReference.Length);
            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private async ValueTask<TValue> StartAsync<TValue>(
            CancellationToken cancellationToken)
        {
            if (StartException is not null)
            {
                throw StartException;
            }

            StartCount++;
            StartToken = cancellationToken;
            _startRequested?.SetResult();
            if (_startCompletion is not null)
            {
                await _startCompletion.Task;
            }

            if (_startFailure is not null)
            {
                return (TValue)(object)_startFailure;
            }

            return default!;
        }

        private Task InvokeCallbackAsync(string methodName, params object[] args)
        {
            var callbackReference = _callbacks
                ?? throw new InvalidOperationException("Audio callbacks were not initialized.");
            var callback = callbackReference.GetType()
                .GetProperty("Value")!
                .GetValue(callbackReference)!;
            return (Task)callback.GetType()
                .GetMethod(methodName)!
                .Invoke(callback, args)!;
        }
    }

    private sealed class TestSpeechRecognizer : IJSObjectReference
    {
        private object? _callbacks;
        private bool _failStart;

        internal bool CreationFails { get; private set; }

        internal int StartCount { get; private set; }

        internal int StopCount { get; private set; }

        internal void FailCreation()
        {
            CreationFails = true;
        }

        internal void FailStart()
        {
            _failStart = true;
        }

        internal void SetCallbacks(object callbacks)
        {
            _callbacks = callbacks;
        }

        internal Task EmitResultAsync(string finalTranscript, string interimTranscript)
        {
            return InvokeCallbackAsync(
                "OnResultAsync",
                finalTranscript,
                interimTranscript);
        }

        internal Task EmitStartedAsync()
        {
            return InvokeCallbackAsync("OnStartedAsync");
        }

        internal Task EmitErrorAsync(string error, bool isFatal)
        {
            return InvokeCallbackAsync("OnErrorAsync", error, isFatal);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "start")
            {
                StartCount++;
                if (_failStart)
                {
                    throw new JSException("Speech recognition failed to start.");
                }
            }
            else if (identifier == "stop")
            {
                StopCount++;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private Task InvokeCallbackAsync(string methodName, params object[] args)
        {
            var callbackReference = _callbacks
                ?? throw new InvalidOperationException("Speech callbacks were not initialized.");
            var callback = callbackReference.GetType()
                .GetProperty("Value")!
                .GetValue(callbackReference)!;
            return (Task)callback.GetType()
                .GetMethod(methodName)!
                .Invoke(callback, args)!;
        }
    }

    private sealed class TestStreamReference : IJSStreamReference
    {
        public long Length => 4;

        internal CancellationToken OpenToken { get; private set; }

        public ValueTask<Stream> OpenReadStreamAsync(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default)
        {
            OpenToken = cancellationToken;
            return ValueTask.FromResult<Stream>(
                new MemoryStream([1, 2, 3, 4], writable: false));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
