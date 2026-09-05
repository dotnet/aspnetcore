// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using System.Linq;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Records audio in the browser and transcribes or attaches it to the nearest
/// <see cref="MessageInput"/>.
/// </summary>
public sealed class AudioCaptureButton : ComponentBase, IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Microsoft.AspNetCore.Components.AI/ai-chat.js";

    private readonly SpeechCallbacks _speechCallbacks;
    private DotNetObjectReference<SpeechCallbacks>? _speechCallbackReference;
    private IJSObjectReference? _module;
    private IJSObjectReference? _recorder;
    private IJSObjectReference? _speechRecognizer;
    private MessageInputContext? _subscribedContext;
    private IDisposable? _changeSubscription;
    private CancellationTokenSource? _operationCts;
    private string _dictationPrefix = string.Empty;
    private string _committedTranscript = string.Empty;
    private bool _isRecording;
    private bool _isTranscribing;
    private bool _isDictating;
    private bool _isSupported = true;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of <see cref="AudioCaptureButton"/>.
    /// </summary>
    public AudioCaptureButton()
    {
        _speechCallbacks = new SpeechCallbacks(this);
    }

    /// <summary>
    /// Gets or sets the nearest message input.
    /// </summary>
    [CascadingParameter]
    public MessageInputContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime used to access browser recording APIs.
    /// </summary>
    [Inject]
    internal IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the maximum recording size in bytes.
    /// </summary>
    [Parameter]
    public long MaximumBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets whether a new recording replaces existing audio attachments.
    /// </summary>
    [Parameter]
    public bool ReplaceExistingAudio { get; set; } = true;

    /// <summary>
    /// Gets or sets whether captured audio is added to the outgoing message.
    /// </summary>
    [Parameter]
    public bool AttachRecording { get; set; } = true;

    /// <summary>
    /// Gets or sets whether browser speech recognition updates the composer while recording.
    /// </summary>
    [Parameter]
    public bool ShowInterimTranscript { get; set; }

    /// <summary>
    /// Gets or sets the browser speech-recognition language. The browser default is used when omitted.
    /// </summary>
    [Parameter]
    public string? SpeechRecognitionLanguage { get; set; }

    /// <summary>
    /// Gets or sets the accessible label shown before recording starts.
    /// </summary>
    [Parameter]
    public string StartLabel { get; set; } = "Record audio";

    /// <summary>
    /// Gets or sets the accessible label shown while recording.
    /// </summary>
    [Parameter]
    public string StopLabel { get; set; } = "Stop recording";

    /// <summary>
    /// Gets or sets custom button content based on whether recording is active.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when audio has been captured.
    /// </summary>
    [Parameter]
    public EventCallback<DataContent> OnRecorded { get; set; }

    /// <summary>
    /// Gets or sets an optional callback that transcribes captured audio into composer text.
    /// </summary>
    [Parameter]
    public Func<DataContent, CancellationToken, ValueTask<string?>>? Transcribe { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked after captured audio has been transcribed.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnTranscribed { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the recording button.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribedContext, Context))
        {
            return;
        }

        _changeSubscription?.Dispose();
        _subscribedContext = Context;
        _changeSubscription = Context.RegisterOnChanged(
            () => _ = InvokeAsync(StateHasChanged));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var isActive = _isRecording || _isTranscribing;
        var disabled = !_isSupported ||
            (!isActive && (Context.IsConversationBusy || Context.IsComposing));
        var label = isActive ? StopLabel : StartLabel;

        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "class", CssClass());
        builder.AddAttribute(4, "disabled", disabled);
        builder.AddAttribute(5, "aria-label", label);
        builder.AddAttribute(6, "aria-pressed", isActive ? "true" : "false");
        builder.AddAttribute(
            7,
            "onclick",
            EventCallback.Factory.Create(this, ToggleRecordingAsync));

        if (ChildContent is not null)
        {
            builder.AddContent(8, ChildContent(isActive));
        }
        else
        {
            builder.AddContent(9, label);
        }

        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _isSupported = await _module.InvokeAsync<bool>("isAudioCaptureSupported");
            if (!_isSupported)
            {
                Context.SetErrorMessage("Audio recording is not supported by this browser.");
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (JSException)
        {
            _isSupported = false;
            Context.SetErrorMessage("Audio recording could not be initialized.");
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task ToggleRecordingAsync()
    {
        if (_isTranscribing)
        {
            CancelTranscription();
            return Task.CompletedTask;
        }

        return _isRecording ? StopRecordingAsync() : StartRecordingAsync();
    }

    private async Task StartRecordingAsync()
    {
        _operationCts?.Cancel();
        var operationCts = new CancellationTokenSource();
        _operationCts = operationCts;
        Context.SetErrorMessage(null);

        try
        {
            _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _recorder ??= await _module.InvokeAsync<IJSObjectReference>(
                "createAudioRecorder",
                MaximumBytes);
            await _recorder.InvokeVoidAsync("start");
            if (!ReferenceEquals(_operationCts, operationCts) ||
                operationCts.IsCancellationRequested)
            {
                operationCts.Dispose();
                return;
            }

            _isRecording = true;
            Context.SetComposing(true);
            Context.SetStatusMessage("Recording audio.");
            await StartInterimTranscriptionAsync();
        }
        catch (JSException)
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                _operationCts = null;
                _isRecording = false;
                Context.SetComposing(false);
                Context.SetErrorMessage(
                    "Microphone access was not available. Check browser permissions.");
            }

            operationCts.Dispose();
        }
    }

    private async Task StopRecordingAsync()
    {
        var operationCts = _operationCts
            ?? throw new InvalidOperationException(
                "Audio recording does not have an active operation.");
        var cancellationToken = operationCts.Token;
        _isRecording = false;
        _isTranscribing = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var hadInterimTranscript = _isDictating;
            await StopInterimTranscriptionAsync();
            var recording = await _recorder!.InvokeAsync<AudioCaptureResult>("stop");
            cancellationToken.ThrowIfCancellationRequested();

            if (recording.TooLarge || recording.Size > MaximumBytes)
            {
                if (recording.StreamReference is not null)
                {
                    await recording.StreamReference.DisposeAsync();
                }
                Context.SetErrorMessage(
                    $"Audio recordings must be {FormatBytes(MaximumBytes)} or smaller.");
                return;
            }

            if (recording.StreamReference is null || recording.Size == 0)
            {
                Context.SetErrorMessage(
                    "The browser did not capture audio. Record for at least one second and check the microphone input level.");
                return;
            }

            var mediaType = string.IsNullOrWhiteSpace(recording.MimeType)
                ? "audio/webm"
                : recording.MimeType;
            await using var streamReference = recording.StreamReference;
            await using var stream = await streamReference.OpenReadStreamAsync(
                MaximumBytes,
                cancellationToken);
            var content = await DataContent.LoadFromAsync(
                stream,
                mediaType,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            content.Name =
                $"recording-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.{GetExtension(mediaType)}";

            if (AttachRecording && ReplaceExistingAudio)
            {
                foreach (var attachment in Context.Attachments
                    .Where(attachment => attachment.HasTopLevelMediaType("audio"))
                    .ToArray())
                {
                    await Context.RemoveAttachmentAsync(attachment);
                }
            }

            if (AttachRecording)
            {
                await Context.AddAttachmentAsync(content);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await OnRecorded.InvokeAsync(content);
            if (!ReferenceEquals(_operationCts, operationCts))
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (Transcribe is not null)
            {
                Context.SetStatusMessage("Transcribing audio.");
                var transcript = await Transcribe(content, cancellationToken);
                if (!ReferenceEquals(_operationCts, operationCts))
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(transcript))
                {
                    Context.Text = hadInterimTranscript
                        ? AppendText(_dictationPrefix, transcript.Trim())
                        : AppendText(Context.Text, transcript.Trim());
                    await OnTranscribed.InvokeAsync(transcript.Trim());
                    Context.SetStatusMessage("Voice transcription ready.");
                }
                else
                {
                    Context.SetErrorMessage("No speech was recognized in the recording.");
                }
            }
            else if (AttachRecording)
            {
                Context.SetStatusMessage("Audio recording attached.");
            }
            else
            {
                Context.SetErrorMessage(
                    "A transcription callback is required when recordings are not attached.");
            }
            await Context.FocusAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (JSException)
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                Context.SetErrorMessage("The audio recording could not be completed.");
            }
        }
        catch (IOException)
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                Context.SetErrorMessage("The captured audio could not be read.");
            }
        }
        catch (InvalidOperationException exception)
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                Context.SetErrorMessage(exception.Message);
            }
        }
        finally
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                _operationCts = null;
                _isRecording = false;
                _isTranscribing = false;
                if (!_isDisposed)
                {
                    Context.SetComposing(false);
                }
            }

            operationCts.Dispose();
        }
    }

    private void CancelTranscription()
    {
        _operationCts?.Cancel();
        _isTranscribing = false;
        Context.SetComposing(false);
        Context.SetStatusMessage("Audio transcription canceled.");
    }

    private async Task StartInterimTranscriptionAsync()
    {
        if (!ShowInterimTranscript || _module is null)
        {
            return;
        }

        try
        {
            if (!await _module.InvokeAsync<bool>(
                "isLiveSpeechRecognitionSupported"))
            {
                return;
            }

            _speechCallbackReference ??= DotNetObjectReference.Create(_speechCallbacks);
            _speechRecognizer ??= await _module.InvokeAsync<IJSObjectReference>(
                "createLiveSpeechRecognizer",
                _speechCallbackReference,
                SpeechRecognitionLanguage);
            _dictationPrefix = Context.Text.Trim();
            _committedTranscript = string.Empty;
            _isDictating = true;
            await _speechRecognizer.InvokeVoidAsync("start");
            Context.SetStatusMessage("Recording and transcribing.");
        }
        catch (JSException)
        {
            _isDictating = false;
            Context.SetStatusMessage("Recording audio. Live transcription is unavailable.");
        }
    }

    private async Task StopInterimTranscriptionAsync()
    {
        if (!_isDictating || _speechRecognizer is null)
        {
            return;
        }

        _isDictating = false;
        try
        {
            await _speechRecognizer.InvokeVoidAsync("stop");
        }
        catch (JSException)
        {
            Context.SetStatusMessage("Transcribing the completed recording.");
        }
    }

    private Task HandleSpeechResultAsync(
        string finalTranscript,
        string interimTranscript)
    {
        return InvokeAsync(() =>
        {
            if (!_isRecording || !_isDictating)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(finalTranscript))
            {
                _committedTranscript =
                    AppendText(_committedTranscript, finalTranscript);
            }

            Context.Text = AppendText(
                AppendText(_dictationPrefix, _committedTranscript),
                interimTranscript);
            Context.SetStatusMessage("Recording and transcribing.");
        });
    }

    private Task HandleSpeechErrorAsync()
    {
        return InvokeAsync(() =>
        {
            _isDictating = false;
            if (_isRecording)
            {
                Context.SetStatusMessage(
                    "Recording audio. Live transcription is unavailable.");
            }
        });
    }

    private string CssClass()
    {
        var css = _isRecording
            ? "sc-ai-input__audio sc-ai-input__audio--recording"
            : "sc-ai-input__audio";
        if (AdditionalAttributes?.TryGetValue("class", out var value) == true &&
            value is string additionalClass)
        {
            css = $"{css} {additionalClass}";
        }

        return css;
    }

    private static string GetExtension(string mediaType)
    {
        if (mediaType.Contains("ogg", StringComparison.OrdinalIgnoreCase))
        {
            return "ogg";
        }

        if (mediaType.Contains("mp4", StringComparison.OrdinalIgnoreCase))
        {
            return "m4a";
        }

        if (mediaType.Contains("wav", StringComparison.OrdinalIgnoreCase))
        {
            return "wav";
        }

        return "webm";
    }

    private static string FormatBytes(long bytes)
    {
        const long megabyte = 1024 * 1024;
        return bytes >= megabyte && bytes % megabyte == 0
            ? $"{bytes / megabyte} MB"
            : $"{bytes} bytes";
    }

    private static string AppendText(string existingText, string transcript)
    {
        return string.IsNullOrWhiteSpace(existingText)
            ? transcript
            : $"{existingText.TrimEnd()} {transcript}";
    }

    /// <summary>
    /// Stops recording and releases browser resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _changeSubscription?.Dispose();
        _operationCts?.Cancel();
        if (!_isTranscribing)
        {
            _operationCts?.Dispose();
            _operationCts = null;
        }
        Context.SetComposing(false);

        if (_recorder is not null)
        {
            try
            {
                await _recorder.InvokeVoidAsync("dispose");
                await _recorder.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        if (_speechRecognizer is not null)
        {
            try
            {
                await _speechRecognizer.InvokeVoidAsync("dispose");
                await _speechRecognizer.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _speechCallbackReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class SpeechCallbacks(AudioCaptureButton owner)
    {
        [JSInvokable]
        public Task OnResultAsync(string finalTranscript, string interimTranscript)
        {
            return owner.HandleSpeechResultAsync(finalTranscript, interimTranscript);
        }

        [JSInvokable]
        public Task OnErrorAsync(string _, bool __)
        {
            return owner.HandleSpeechErrorAsync();
        }
    }

    private sealed class AudioCaptureResult
    {
        public IJSStreamReference? StreamReference { get; set; }

        public string MimeType { get; set; } = string.Empty;

        public long Size { get; set; }

        public bool TooLarge { get; set; }
    }
}
