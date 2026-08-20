// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Captures continuous browser speech recognition, shows interim text in the nearest
/// <see cref="MessageInput"/>, and optionally submits each completed utterance.
/// </summary>
public sealed class LiveSpeechButton : ComponentBase, IAsyncDisposable
{
    private const string ModulePath =
        "./_content/Microsoft.AspNetCore.Components.AI/ai-chat.js";

    private readonly SpeechCallbacks _callbacks;
    private DotNetObjectReference<SpeechCallbacks>? _callbackReference;
    private IJSObjectReference? _module;
    private IJSObjectReference? _recognizer;
    private MessageInputContext? _subscribedContext;
    private IDisposable? _changeSubscription;
    private string _prefix = string.Empty;
    private string _committedTranscript = string.Empty;
    private bool _isEnabled;
    private bool _isListening;
    private bool _isStarting;
    private bool _isFinalizing;
    private bool _isSupported = true;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of <see cref="LiveSpeechButton"/>.
    /// </summary>
    public LiveSpeechButton()
    {
        _callbacks = new SpeechCallbacks(this);
    }

    /// <summary>
    /// Gets or sets the nearest message input.
    /// </summary>
    [CascadingParameter]
    public MessageInputContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime used to access browser speech recognition.
    /// </summary>
    [Inject]
    internal IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether each finalized utterance is submitted automatically.
    /// </summary>
    [Parameter]
    public bool AutoSubmit { get; set; } = true;

    /// <summary>
    /// Gets or sets the speech-recognition language. The browser default is used when omitted.
    /// </summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets whether interim speech is displayed in the message composer.
    /// </summary>
    [Parameter]
    public bool ShowInterimInComposer { get; set; } = true;

    /// <summary>
    /// Gets or sets the accessible label shown before live speech starts.
    /// </summary>
    [Parameter]
    public string StartLabel { get; set; } = "Start live voice";

    /// <summary>
    /// Gets or sets the accessible label shown while live speech is enabled.
    /// </summary>
    [Parameter]
    public string StopLabel { get; set; } = "Stop live voice";

    /// <summary>
    /// Gets or sets custom button content based on whether live speech is enabled.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked for each finalized transcript.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnTranscript { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when the visible interim transcript changes.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnInterimTranscript { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the live-speech button.
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
        _changeSubscription = Context.RegisterOnChanged(OnContextChanged);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var disabled = !_isSupported ||
            (!_isEnabled && (Context.IsConversationBusy || Context.IsComposing));
        var label = _isEnabled ? StopLabel : StartLabel;

        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "class", CssClass());
        builder.AddAttribute(4, "disabled", disabled);
        builder.AddAttribute(5, "aria-label", label);
        builder.AddAttribute(6, "aria-pressed", _isEnabled ? "true" : "false");
        builder.AddAttribute(
            7,
            "onclick",
            EventCallback.Factory.Create(this, ToggleAsync));

        if (ChildContent is not null)
        {
            builder.AddContent(8, ChildContent(_isEnabled));
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
            _isSupported = await _module.InvokeAsync<bool>(
                "isLiveSpeechRecognitionSupported");
            if (!_isSupported)
            {
                Context.SetStatusMessage(
                    "Live voice is not supported by this browser. Recorded voice notes are still available.");
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (JSException)
        {
            _isSupported = false;
            Context.SetErrorMessage("Live voice could not be initialized.");
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task ToggleAsync()
    {
        return _isEnabled ? StopAsync() : StartAsync();
    }

    private async Task StartAsync()
    {
        Context.SetErrorMessage(null);

        try
        {
            _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _callbackReference ??= DotNetObjectReference.Create(_callbacks);
            _recognizer ??= await _module.InvokeAsync<IJSObjectReference>(
                "createLiveSpeechRecognizer",
                _callbackReference,
                Language);
            _prefix = Context.Text.Trim();
            _committedTranscript = string.Empty;
            _isEnabled = true;
            await StartListeningAsync();
        }
        catch (JSException)
        {
            _isEnabled = false;
            _isListening = false;
            Context.SetComposing(false);
            Context.SetErrorMessage(
                "Microphone speech recognition was not available. Check browser permissions.");
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
            await _recognizer!.InvokeVoidAsync("start");
            _isListening = true;
            Context.SetComposing(true);
            Context.SetStatusMessage("Listening for your next instruction.");
        }
        catch (JSException)
        {
            _isEnabled = false;
            _isListening = false;
            Context.SetComposing(false);
            Context.SetErrorMessage(
                "Microphone speech recognition was not available. Check browser permissions.");
        }
        finally
        {
            _isStarting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task StopAsync()
    {
        _isEnabled = false;
        _isListening = false;
        Context.SetComposing(false);
        if (_recognizer is not null)
        {
            await _recognizer.InvokeVoidAsync("stop");
        }
        await OnInterimTranscript.InvokeAsync(string.Empty);
        Context.SetStatusMessage("Live voice stopped.");
        await InvokeAsync(StateHasChanged);
    }

    private Task HandleResultAsync(string finalTranscript, string interimTranscript)
    {
        return InvokeAsync(async () =>
        {
            if (!_isEnabled || _isFinalizing)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(finalTranscript))
            {
                _committedTranscript = AppendText(_committedTranscript, finalTranscript);
            }

            var recognizedText =
                AppendText(_committedTranscript, interimTranscript);
            if (ShowInterimInComposer)
            {
                Context.Text = ComposeText(interimTranscript);
            }
            await OnInterimTranscript.InvokeAsync(recognizedText);
            if (string.IsNullOrWhiteSpace(finalTranscript))
            {
                Context.SetStatusMessage("Listening...");
                return;
            }

            await OnTranscript.InvokeAsync(finalTranscript.Trim());
            if (!AutoSubmit)
            {
                Context.SetStatusMessage("Listening for more.");
                return;
            }

            _isFinalizing = true;
            _isListening = false;
            try
            {
                await _recognizer!.InvokeVoidAsync("stop");
                Context.SetComposing(false);
                Context.SetStatusMessage("Sending voice instruction.");
                if (!ShowInterimInComposer)
                {
                    Context.Text = ComposeText(string.Empty);
                }
                if (Context.CanSubmit)
                {
                    var submitTask = Context.SubmitAsync();
                    await OnInterimTranscript.InvokeAsync(string.Empty);
                    await submitTask;
                }

                _prefix = string.Empty;
                _committedTranscript = string.Empty;
            }
            finally
            {
                _isFinalizing = false;
            }

            if (_isEnabled && CanResumeListening)
            {
                await StartListeningAsync();
            }
            else if (_isEnabled)
            {
                Context.SetStatusMessage(
                    "Live voice is on and will resume after the current action.");
                await InvokeAsync(StateHasChanged);
            }
        });
    }

    private void OnContextChanged()
    {
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            if (_isEnabled &&
                !_isListening &&
                !_isFinalizing &&
            CanResumeListening)
            {
                await StartListeningAsync();
            }
        });
    }

    private bool CanResumeListening =>
        Context.Status is ConversationStatus.Idle or ConversationStatus.Error;

    private Task HandleStartedAsync()
    {
        return InvokeAsync(() =>
        {
            if (!_isEnabled)
            {
                return;
            }

            _isListening = true;
            Context.SetComposing(true);
            Context.SetErrorMessage(null);
            Context.SetStatusMessage("Listening for your next instruction.");
            StateHasChanged();
        });
    }

    private Task HandleErrorAsync(string error, bool isFatal)
    {
        return InvokeAsync(async () =>
        {
            _isListening = false;
            if (!isFatal)
            {
                Context.SetStatusMessage(
                    "Live voice was interrupted. Reconnecting automatically.");
                StateHasChanged();
                return;
            }

            _isEnabled = false;
            Context.SetComposing(false);
            await OnInterimTranscript.InvokeAsync(string.Empty);
            Context.SetErrorMessage(error switch
            {
                "not-allowed" or "service-not-allowed" =>
                    "Microphone access was denied. Allow microphone access to use live voice.",
                "language-not-supported" =>
                    "The selected live voice language is not supported by this browser.",
                _ => "Live voice could not continue because speech recognition is not configured correctly.",
            });
            StateHasChanged();
        });
    }

    private string ComposeText(string interimTranscript)
    {
        var text = AppendText(_prefix, _committedTranscript);
        return AppendText(text, interimTranscript);
    }

    private string CssClass()
    {
        var css = _isEnabled
            ? "sc-ai-input__live-speech sc-ai-input__live-speech--active"
            : "sc-ai-input__live-speech";
        if (_isEnabled && !_isListening)
        {
            css += " sc-ai-input__live-speech--waiting";
        }
        if (AdditionalAttributes?.TryGetValue("class", out var value) == true &&
            value is string additionalClass)
        {
            css = $"{css} {additionalClass}";
        }

        return css;
    }

    private static string AppendText(string existingText, string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
        {
            return existingText.Trim();
        }

        return string.IsNullOrWhiteSpace(existingText)
            ? newText.Trim()
            : $"{existingText.TrimEnd()} {newText.Trim()}";
    }

    /// <summary>
    /// Stops recognition and releases browser resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _changeSubscription?.Dispose();
        Context.SetComposing(false);

        if (_recognizer is not null)
        {
            try
            {
                await _recognizer.InvokeVoidAsync("dispose");
                await _recognizer.DisposeAsync();
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

        _callbackReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class SpeechCallbacks(LiveSpeechButton owner)
    {
        [JSInvokable]
        public Task OnStartedAsync()
        {
            return owner.HandleStartedAsync();
        }

        [JSInvokable]
        public Task OnResultAsync(string finalTranscript, string interimTranscript)
        {
            return owner.HandleResultAsync(finalTranscript, interimTranscript);
        }

        [JSInvokable]
        public Task OnErrorAsync(string error, bool isFatal)
        {
            return owner.HandleErrorAsync(error, isFatal);
        }
    }
}
