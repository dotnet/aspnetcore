// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using System.Linq;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Captures speech using browser recognition, backend transcription, or both,
/// and adds the resulting text to the nearest <see cref="MessageInput"/>.
/// </summary>
public sealed class AudioCaptureButton : ComponentBase, IAsyncDisposable
{
    private readonly SpeechCallbacks _speechCallbacks;
    private DotNetObjectReference<SpeechCallbacks>? _speechCallbackReference;
    private AudioCaptureButtonInterop? _interop;
    private MessageInputContext? _subscribedContext;
    private IDisposable? _changeSubscription;
    private CancellationTokenSource? _operationCts;
    private string _dictationPrefix = string.Empty;
    private string _committedTranscript = string.Empty;
    private bool _isEnabled;
    private bool _isListening;
    private bool _isStarting;
    private bool _isFinalizing;
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
    /// Gets or sets the maximum recording size in bytes. This value is ignored
    /// when <see cref="RecognitionMode"/> is <see cref="SpeechRecognitionMode.BrowserSpeechRecognition"/>.
    /// </summary>
    [Parameter]
    public long MaximumBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets whether a new recording replaces existing audio attachments.
    /// This value is ignored when <see cref="RecognitionMode"/> is
    /// <see cref="SpeechRecognitionMode.BrowserSpeechRecognition"/>.
    /// </summary>
    [Parameter]
    public bool ReplaceExistingAudio { get; set; } = true;

    /// <summary>
    /// Gets or sets whether captured audio is added to the outgoing message.
    /// This value is ignored when <see cref="RecognitionMode"/> is
    /// <see cref="SpeechRecognitionMode.BrowserSpeechRecognition"/>.
    /// </summary>
    [Parameter]
    public bool AttachRecording { get; set; } = true;

    /// <summary>
    /// Gets or sets how captured speech is converted to text.
    /// </summary>
    [Parameter]
    public SpeechRecognitionMode RecognitionMode { get; set; } =
        SpeechRecognitionMode.BackendTranscription;

    /// <summary>
    /// Gets or sets whether finalized speech is submitted automatically.
    /// </summary>
    [Parameter]
    public bool AutoSubmit { get; set; }

    /// <summary>
    /// Gets or sets whether browser recognition resumes after an automatically
    /// submitted response completes.
    /// </summary>
    [Parameter]
    public bool ContinuousListening { get; set; }

    /// <summary>
    /// Gets or sets the browser speech-recognition language. The browser default is used when omitted.
    /// </summary>
    [Parameter]
    public string? SpeechRecognitionLanguage { get; set; }

    /// <summary>
    /// Gets or sets the accessible label shown before voice input starts.
    /// </summary>
    [Parameter]
    public string StartLabel { get; set; } = "Record audio";

    /// <summary>
    /// Gets or sets the accessible label shown while voice input is active.
    /// </summary>
    [Parameter]
    public string StopLabel { get; set; } = "Stop recording";

    /// <summary>
    /// Gets or sets the accessible label used before voice input starts.
    /// Defaults to <see cref="StartLabel"/>.
    /// </summary>
    [Parameter]
    public string? StartAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the accessible label used while voice input is active.
    /// Defaults to <see cref="StopLabel"/>.
    /// </summary>
    [Parameter]
    public string? StopAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets custom button content based on whether voice input is active.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when audio has been captured. This callback
    /// is not invoked when <see cref="RecognitionMode"/> is
    /// <see cref="SpeechRecognitionMode.BrowserSpeechRecognition"/>.
    /// </summary>
    [Parameter]
    public EventCallback<DataContent> OnRecorded { get; set; }

    /// <summary>
    /// Gets or sets an optional callback that transcribes captured audio into composer text.
    /// </summary>
    [Parameter]
    public Func<DataContent, CancellationToken, ValueTask<string?>>? Transcribe { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when speech has been finalized.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnTranscribed { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when the visible browser transcript changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnInterimTranscript { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the recording button.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _interop = new AudioCaptureButtonInterop(JSRuntime);
        _speechCallbackReference = DotNetObjectReference.Create(_speechCallbacks);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (RecognitionMode is not SpeechRecognitionMode.BrowserSpeechRecognition &&
            Transcribe is null)
        {
            throw new InvalidOperationException(
                $"{nameof(Transcribe)} is required for backend speech recognition.");
        }

        if (ContinuousListening &&
            RecognitionMode is not SpeechRecognitionMode.BrowserSpeechRecognition)
        {
            throw new InvalidOperationException(
                $"{nameof(ContinuousListening)} can only be used with browser speech recognition.");
        }

        if (ContinuousListening && !AutoSubmit)
        {
            throw new InvalidOperationException(
                $"{nameof(ContinuousListening)} requires {nameof(AutoSubmit)}.");
        }

        if (ReferenceEquals(_subscribedContext, Context))
        {
            return;
        }

        _changeSubscription?.Dispose();
        _subscribedContext = Context;
        _changeSubscription = Context.RegisterOnChanged(OnContextChanged);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!_isSupported)
        {
            return;
        }

        var isActive = _isEnabled || _isRecording || _isTranscribing;
        var disabled = !_isSupported ||
            (!isActive && (Context.IsConversationBusy || Context.IsComposing));
        var label = isActive ? StopLabel : StartLabel;
        var ariaLabel = isActive
            ? StopAriaLabel ?? StopLabel
            : StartAriaLabel ?? StartLabel;

        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "class", CssClass());
        builder.AddAttribute(4, "disabled", disabled);
        builder.AddAttribute(5, "aria-label", ariaLabel);
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
            _isSupported = RecognitionMode is SpeechRecognitionMode.BrowserSpeechRecognition
                ? await _interop!.IsSpeechRecognitionSupportedAsync()
                : await _interop!.IsAudioCaptureSupportedAsync();
            StateHasChanged();
        }
        catch (JSException exception)
        {
            _isSupported = false;
            Context.SetErrorMessage(
                $"Audio recording could not be initialized. {exception.Message}");
            StateHasChanged();
        }
    }

    private Task ToggleRecordingAsync()
    {
        if (_isTranscribing)
        {
            CancelTranscription();
            return Task.CompletedTask;
        }

        if (RecognitionMode is SpeechRecognitionMode.BrowserSpeechRecognition)
        {
            return _isEnabled ? StopBrowserRecognitionAsync() : StartBrowserRecognitionAsync();
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
            await _interop!.StartRecordingAsync(
                MaximumBytes,
                _speechCallbackReference!,
                operationCts.Token);
            if (!ReferenceEquals(_operationCts, operationCts) ||
                operationCts.IsCancellationRequested)
            {
                operationCts.Dispose();
                return;
            }

            _isRecording = true;
            _isEnabled = true;
            Context.SetComposing(true);
            Context.SetStatusMessage("Recording audio.");
            if (RecognitionMode is SpeechRecognitionMode.BrowserSpeechRecognitionWithBackendTranscription)
            {
                await StartBrowserDraftAsync();
            }
        }
        catch (JSException exception)
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                _operationCts = null;
                _isRecording = false;
                Context.SetComposing(false);
                Context.SetErrorMessage(
                    $"Microphone access was not available. {exception.Message}");
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
        StateHasChanged();

        try
        {
            var hadInterimTranscript = _isDictating;
            await StopInterimTranscriptionAsync();
            var recording = await _interop!.StopRecordingAsync(cancellationToken);
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

            if (string.IsNullOrWhiteSpace(recording.MimeType))
            {
                throw new InvalidOperationException(
                    "The browser did not provide the recorded audio MIME type.");
            }

            var mediaType = recording.MimeType;
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
                    if (AutoSubmit)
                    {
                        _isTranscribing = false;
                        Context.SetComposing(false);
                        if (Context.CanSubmit)
                        {
                            Context.SetStatusMessage("Sending voice instruction.");
                            await Context.SubmitAsync();
                        }
                    }
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
        catch (JSException exception)
        {
            if (ReferenceEquals(_operationCts, operationCts))
            {
                Context.SetErrorMessage(
                    $"The audio recording could not be completed. {exception.Message}");
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
                _isEnabled = false;
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

    private async Task StartBrowserDraftAsync()
    {
        if (_interop is null)
        {
            return;
        }

        try
        {
            if (!await _interop.IsSpeechRecognitionSupportedAsync())
            {
                return;
            }

            await _interop.InitializeSpeechRecognitionAsync(
                _speechCallbackReference!,
                SpeechRecognitionLanguage,
                _operationCts?.Token ?? default);
            await _interop.StartSpeechRecognitionAsync(_operationCts?.Token ?? default);
            _dictationPrefix = Context.Text.Trim();
            _committedTranscript = string.Empty;
            _isDictating = true;
            _isListening = true;
            Context.SetStatusMessage("Recording and transcribing.");
        }
        catch (JSException exception)
        {
            _isDictating = false;
            _isListening = false;
            Context.SetStatusMessage(
                $"Recording audio. Live transcription is unavailable. {exception.Message}");
        }
    }

    private async Task StartBrowserRecognitionAsync()
    {
        Context.SetErrorMessage(null);
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = new CancellationTokenSource();
        var cancellationToken = _operationCts.Token;

        try
        {
            await _interop!.InitializeSpeechRecognitionAsync(
                _speechCallbackReference!,
                SpeechRecognitionLanguage,
                cancellationToken);
            _dictationPrefix = Context.Text.Trim();
            _committedTranscript = string.Empty;
            _isEnabled = true;
            _isDictating = true;
            await StartListeningAsync();
        }
        catch (JSException exception)
        {
            _isEnabled = false;
            _isListening = false;
            _isDictating = false;
            Context.SetComposing(false);
            Context.SetErrorMessage(
                $"Microphone speech recognition was not available. {exception.Message}");
        }
    }

    private async Task StartListeningAsync()
    {
        if (!_isEnabled || _isListening || _isStarting)
        {
            return;
        }

        _isStarting = true;
        try
        {
            await _interop!.StartSpeechRecognitionAsync(_operationCts?.Token ?? default);
            _isListening = true;
            Context.SetComposing(true);
            Context.SetStatusMessage("Listening for your next instruction.");
        }
        catch (JSException exception)
        {
            _isEnabled = false;
            _isListening = false;
            _isDictating = false;
            Context.SetComposing(false);
            Context.SetErrorMessage(
                $"Microphone speech recognition was not available. {exception.Message}");
        }
        finally
        {
            _isStarting = false;
            StateHasChanged();
        }
    }

    private async Task StopBrowserRecognitionAsync()
    {
        _isEnabled = false;
        _isListening = false;
        _isDictating = false;
        Context.SetComposing(false);
        if (_interop is not null)
        {
            await _interop.StopSpeechRecognitionAsync();
        }

        await OnInterimTranscript.InvokeAsync(string.Empty);
        Context.SetStatusMessage("Voice input stopped.");
        StateHasChanged();
    }

    private async Task StopInterimTranscriptionAsync()
    {
        if (!_isDictating || _interop is null)
        {
            return;
        }

        _isDictating = false;
        _isListening = false;
        try
        {
            await _interop.StopSpeechRecognitionAsync();
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
        return InvokeAsync(async () =>
        {
            if (!_isEnabled || !_isDictating || _isFinalizing)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(finalTranscript))
            {
                _committedTranscript =
                    AppendText(_committedTranscript, finalTranscript);
            }

            var recognizedText = AppendText(_committedTranscript, interimTranscript);
            Context.Text = AppendText(
                AppendText(_dictationPrefix, _committedTranscript),
                interimTranscript);
            await OnInterimTranscript.InvokeAsync(recognizedText);
            if (string.IsNullOrWhiteSpace(finalTranscript))
            {
                Context.SetStatusMessage(
                    _isRecording ? "Recording and transcribing." : "Listening...");
                return;
            }

            await OnTranscribed.InvokeAsync(finalTranscript.Trim());
            if (RecognitionMode is not SpeechRecognitionMode.BrowserSpeechRecognition || !AutoSubmit)
            {
                Context.SetStatusMessage(
                    _isRecording ? "Recording and transcribing." : "Listening for more.");
                return;
            }

            _isFinalizing = true;
            _isListening = false;
            try
            {
                await _interop!.StopSpeechRecognitionAsync();
                Context.SetComposing(false);
                Context.SetStatusMessage("Sending voice instruction.");
                if (Context.CanSubmit)
                {
                    var submitTask = Context.SubmitAsync();
                    await OnInterimTranscript.InvokeAsync(string.Empty);
                    await submitTask;
                }

                _dictationPrefix = string.Empty;
                _committedTranscript = string.Empty;
            }
            finally
            {
                _isFinalizing = false;
            }

            if (ContinuousListening && _isEnabled && !_isDisposed)
            {
                _isDictating = true;
                await StartListeningAsync();
            }
            else
            {
                _isEnabled = false;
                _isDictating = false;
            }
        });
    }

    private Task HandleSpeechStartedAsync()
    {
        return InvokeAsync(() =>
        {
            if (!_isEnabled || _isFinalizing)
            {
                return;
            }

            _isListening = true;
            _isDictating = true;
            Context.SetComposing(true);
            Context.SetErrorMessage(null);
            Context.SetStatusMessage("Listening for your next instruction.");
            StateHasChanged();
        });
    }

    private void OnContextChanged()
    {
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            if (RecognitionMode is SpeechRecognitionMode.BrowserSpeechRecognition &&
                ContinuousListening &&
                _isEnabled &&
                !_isListening &&
                !_isStarting &&
                !_isFinalizing &&
                Context.Status is ConversationStatus.Idle or ConversationStatus.Error)
            {
                _isDictating = true;
                await StartListeningAsync();
            }
        });
    }

    private Task HandleSpeechErrorAsync(string error, bool isFatal)
    {
        return InvokeAsync(async () =>
        {
            _isListening = false;
            if (_isRecording)
            {
                _isDictating = false;
                Context.SetStatusMessage(
                    "Recording audio. Live transcription is unavailable.");
                StateHasChanged();
                return;
            }

            if (!isFatal)
            {
                Context.SetStatusMessage(
                    "Voice input was interrupted. Reconnecting automatically.");
                StateHasChanged();
                return;
            }

            _isEnabled = false;
            _isDictating = false;
            Context.SetComposing(false);
            await OnInterimTranscript.InvokeAsync(string.Empty);
            Context.SetErrorMessage(error switch
            {
                "not-allowed" or "service-not-allowed" =>
                    "Microphone access was denied. Allow microphone access to use voice input.",
                "language-not-supported" =>
                    "The selected speech recognition language is not supported by this browser.",
                _ => "Voice input could not continue because speech recognition is not configured correctly.",
            });
            StateHasChanged();
        });
    }

    private Task HandleRecordingErrorAsync(string error)
    {
        return InvokeAsync(async () =>
        {
            if (!_isRecording)
            {
                return;
            }

            _operationCts?.Cancel();
            _operationCts?.Dispose();
            _operationCts = null;
            _isEnabled = false;
            _isRecording = false;
            _isTranscribing = false;
            _isDictating = false;
            _isListening = false;
            Context.SetComposing(false);
            if (_interop is not null)
            {
                await _interop.StopSpeechRecognitionAsync();
            }

            await OnInterimTranscript.InvokeAsync(string.Empty);
            Context.SetErrorMessage(error is "permission-revoked" or "not-allowed"
                ? "Microphone access was revoked. Allow microphone access to record audio."
                : "Audio recording stopped because the microphone became unavailable.");
            StateHasChanged();
        });
    }

    private string CssClass()
    {
        var css = _isEnabled
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
        if (string.IsNullOrWhiteSpace(existingText))
        {
            return transcript;
        }

        return string.IsNullOrWhiteSpace(transcript)
            ? existingText.TrimEnd()
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

        if (_interop is not null)
        {
            await _interop.DisposeAsync();
        }

        _speechCallbackReference?.Dispose();
    }

    private sealed class SpeechCallbacks(AudioCaptureButton owner)
    {
        [JSInvokable]
        public Task OnResultAsync(string finalTranscript, string interimTranscript)
        {
            return owner.HandleSpeechResultAsync(finalTranscript, interimTranscript);
        }

        [JSInvokable]
        public Task OnStartedAsync()
        {
            return owner.HandleSpeechStartedAsync();
        }

        [JSInvokable]
        public Task OnErrorAsync(string error, bool isFatal)
        {
            return owner.HandleSpeechErrorAsync(error, isFatal);
        }

        [JSInvokable]
        public Task OnRecordingErrorAsync(string error)
        {
            return owner.HandleRecordingErrorAsync(error);
        }
    }

}
