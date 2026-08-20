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
        await cut.InvokeAsync(() => ClickAsync(button));
        await stopTask;

        Assert.True(transcriptionToken.CanBeCanceled);
        Assert.True(transcriptionToken.IsCancellationRequested);
        Assert.True(recorder.StreamReference.OpenToken.CanBeCanceled);
        Assert.Equal(1, recorder.StopCount);
        Assert.False(input.IsComposing);
        Assert.Null(input.ErrorMessage);
        Assert.Equal("Audio transcription canceled.", input.StatusMessage);
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
            Func<DataContent, CancellationToken, ValueTask<string?>> transcribe)
    {
        var recorder = new TestAudioRecorder();
        var services = new TestServiceProvider();
        services.AddService<IJSRuntime>(new TestJSRuntime(new TestAudioModule(recorder)));
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
                    nameof(MessageInput.TopContent),
                    (RenderFragment<MessageInputContext>)(context => childBuilder =>
                    {
                        input = context;
                    }));
                builder.AddComponentParameter(
                    2,
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

    private sealed class TestAudioModule(TestAudioRecorder recorder) : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => identifier == "createAudioRecorder"
                ? ValueTask.FromResult((TValue)(object)recorder)
                : ValueTask.FromResult(default(TValue)!);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestAudioRecorder : IJSObjectReference
    {
        internal TestStreamReference StreamReference { get; } = new();

        internal int StopCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
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
