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
            TestSpeechRecognizer? speechRecognizer = null)
    {
        var recorder = new TestAudioRecorder();
        var services = new TestServiceProvider();
        services.AddService<IJSRuntime>(
            new TestJSRuntime(
                new TestAudioModule(
                    recorder,
                    speechRecognizer ?? new TestSpeechRecognizer())));
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
            .Single(frame =>
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
        TestSpeechRecognizer speechRecognizer) : IJSObjectReference
    {
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
                "isAudioCaptureSupported" or "isLiveSpeechRecognitionSupported" =>
                    ValueTask.FromResult((TValue)(object)true),
                "createAudioRecorder" =>
                    ValueTask.FromResult((TValue)(object)recorder),
                "createLiveSpeechRecognizer" =>
                    CreateSpeechRecognizer<TValue>(args),
                _ => ValueTask.FromResult(default(TValue)!),
            };
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private ValueTask<TValue> CreateSpeechRecognizer<TValue>(object?[]? args)
        {
            speechRecognizer.SetCallbacks(args![0]!);
            return ValueTask.FromResult((TValue)(object)speechRecognizer);
        }
    }

    private sealed class TestAudioRecorder : IJSObjectReference
    {
        internal TestStreamReference StreamReference { get; } = new();

        internal int StopCount { get; private set; }

        internal int StartCount { get; private set; }

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
                return ValueTask.FromResult(default(TValue)!);
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
                .SetValue(result, "audio/webm");
            typeof(TValue).GetProperty("Size")!
                .SetValue(result, StreamReference.Length);
            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestSpeechRecognizer : IJSObjectReference
    {
        private object? _callbacks;

        internal int StartCount { get; private set; }

        internal int StopCount { get; private set; }

        internal void SetCallbacks(object callbacks)
        {
            _callbacks = callbacks;
        }

        internal Task EmitResultAsync(string finalTranscript, string interimTranscript)
        {
            var callbackReference = _callbacks
                ?? throw new InvalidOperationException("Speech callbacks were not initialized.");
            var callback = callbackReference.GetType()
                .GetProperty("Value")!
                .GetValue(callbackReference)!;
            return (Task)callback.GetType()
                .GetMethod("OnResultAsync")!
                .Invoke(callback, [finalTranscript, interimTranscript])!;
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
            }
            else if (identifier == "stop")
            {
                StopCount++;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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
