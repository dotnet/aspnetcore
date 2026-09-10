// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Composes text and binary content and sends messages to the cascaded
/// <see cref="AgentContext"/>.
/// </summary>
public sealed class MessageInput : ComponentBase, IDisposable, IAsyncDisposable
{
    private readonly MessageInputContext _context;
    private readonly List<DataContent> _attachments = [];
    private readonly string _microphonePermissionId =
        $"sc-ai-input-microphone-permission-{Guid.NewGuid():N}";
    private readonly string _statusId = $"sc-ai-input-status-{Guid.NewGuid():N}";
    private readonly string _errorId = $"sc-ai-input-error-{Guid.NewGuid():N}";
    private object? _microphonePermissionOwner;
    private AgentContext? _subscribedContext;
    private IDisposable? _statusSubscription;
    private MessageInputInterop? _interop;
    private DotNetObjectReference<KeyboardCallbacks>? _keyboardCallbacksReference;
    private ElementReference _textArea;
    private string _text = string.Empty;
    private ConversationStatus _status;
    private bool _isComposing;
    private bool _keyboardBusy;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of <see cref="MessageInput"/>.
    /// </summary>
    public MessageInput()
    {
        _context = new MessageInputContext(this);
    }

    /// <summary>
    /// Gets or sets the conversation this input sends messages to.
    /// </summary>
    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime used to provide immediate keyboard handling.
    /// </summary>
    [Inject]
    internal IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the placeholder text of the input.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the text area.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets an additional class applied to the text area.
    /// </summary>
    [Parameter]
    public string? TextAreaClass { get; set; }

    /// <summary>
    /// Gets or sets the content rendered above the attachments and text area.
    /// </summary>
    [Parameter]
    public RenderFragment<MessageInputContext>? TopContent { get; set; }

    /// <summary>
    /// Gets or sets content that replaces the default microphone permission feedback.
    /// Custom content is responsible for communicating status changes accessibly,
    /// such as by providing appropriate live-region semantics.
    /// </summary>
    [Parameter]
    public RenderFragment<MessageInputContext>? MicrophonePermissionContent { get; set; }

    /// <summary>
    /// Gets or sets the message displayed while the browser waits for a microphone
    /// permission decision.
    /// </summary>
    [Parameter]
    public string RequestingMicrophonePermissionMessage { get; set; } =
        "Waiting for microphone permission...";

    /// <summary>
    /// Gets or sets the message displayed when microphone access is denied or revoked.
    /// </summary>
    [Parameter]
    public string MicrophonePermissionDeniedMessage { get; set; } =
        "Microphone access is blocked. Enable it in your browser settings to use voice input.";

    /// <summary>
    /// Gets or sets the message displayed when no microphone is available.
    /// </summary>
    [Parameter]
    public string MicrophoneUnavailableMessage { get; set; } =
        "No microphone is available.";

    /// <summary>
    /// Gets or sets the attachment content. The default is a
    /// <see cref="MessageAttachmentList"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<MessageInputContext>? AttachmentContent { get; set; }

    /// <summary>
    /// Gets or sets the content rendered before the text area.
    /// </summary>
    [Parameter]
    public RenderFragment? LeadingActions { get; set; }

    /// <summary>
    /// Gets or sets content rendered after the text area in place of the default send or stop
    /// button.
    /// </summary>
    [Parameter]
    public RenderFragment? TrailingActions { get; set; }

    /// <summary>
    /// Gets or sets content rendered below the composer.
    /// </summary>
    [Parameter]
    public RenderFragment<MessageInputContext>? BottomContent { get; set; }

    /// <summary>
    /// Gets or sets whether the default send button is shown.
    /// </summary>
    [Parameter]
    public bool ShowDefaultSendButton { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the default stop button is shown.
    /// </summary>
    [Parameter]
    public bool ShowDefaultStopButton { get; set; } = true;

    /// <summary>
    /// Gets or sets the text used when a message contains attachments but no entered text.
    /// </summary>
    [Parameter]
    public string AttachmentOnlyText { get; set; } = "Review the attached files.";

    /// <summary>
    /// Gets or sets the accessible label for the default send button.
    /// </summary>
    [Parameter]
    public string SendLabel { get; set; } = "Send message";

    /// <summary>
    /// Gets or sets the accessible label for the default stop button.
    /// </summary>
    [Parameter]
    public string StopLabel { get; set; } = "Stop response";

    /// <summary>
    /// Gets or sets a callback invoked immediately before a message is sent.
    /// </summary>
    [Parameter]
    public EventCallback<ChatMessage> OnSubmitted { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked after the current response is stopped.
    /// </summary>
    [Parameter]
    public EventCallback OnCanceled { get; set; }

    /// <summary>
    /// Gets or sets additional attributes applied to the root form.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    internal string Text => _text;

    internal IReadOnlyList<DataContent> Attachments => _attachments;

    internal ConversationStatus Status => _status;

    internal bool IsConversationBusy =>
        _status is ConversationStatus.Streaming or ConversationStatus.AwaitingInput;

    internal bool IsComposing => _isComposing;

    internal bool CanCancel => IsConversationBusy;

    internal bool CanSubmit =>
        !IsConversationBusy &&
        !_isComposing &&
        (!string.IsNullOrWhiteSpace(_text) || _attachments.Count > 0);

    internal string? StatusMessage { get; private set; }

    internal string? ErrorMessage { get; private set; }

    internal MicrophonePermissionStatus CurrentMicrophonePermissionStatus { get; private set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var newContext = AgentContext
            ?? throw new InvalidOperationException(
                "MessageInput must be inside an AgentBoundary.");

        if (ReferenceEquals(_subscribedContext, newContext))
        {
            return;
        }

        _statusSubscription?.Dispose();
        _subscribedContext = newContext;
        _status = newContext.Status;
        _statusSubscription = newContext.RegisterOnStatusChanged(status =>
        {
            _status = status;
            _ = InvokeAsync(Refresh);
        });
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MessageInputContext>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<MessageInputContext>.Value), _context);
        builder.AddComponentParameter(2, nameof(CascadingValue<MessageInputContext>.IsFixed), true);
        builder.AddComponentParameter(
            3,
            nameof(CascadingValue<MessageInputContext>.ChildContent),
            (RenderFragment)RenderInput);
        builder.CloseComponent();
    }

    private void RenderInput(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "form");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass());
        builder.AddAttribute(
            3,
            "onsubmit",
            EventCallback.Factory.Create(this, SubmitAsync));
        builder.AddEventPreventDefaultAttribute(4, "onsubmit", true);

        if (CurrentMicrophonePermissionStatus is not MicrophonePermissionStatus.None)
        {
            if (MicrophonePermissionContent is not null)
            {
                builder.AddContent(5, MicrophonePermissionContent(_context));
            }
            else
            {
                builder.OpenRegion(5);
                RenderDefaultMicrophonePermission(builder);
                builder.CloseRegion();
            }
        }

        if (TopContent is not null)
        {
            builder.AddContent(10, TopContent(_context));
        }

        if (AttachmentContent is not null)
        {
            builder.AddContent(20, AttachmentContent(_context));
        }
        else if (_attachments.Count > 0)
        {
            builder.OpenComponent<MessageAttachmentList>(20);
            builder.CloseComponent();
        }

        builder.OpenElement(30, "div");
        builder.AddAttribute(31, "class", "sc-ai-input");

        if (LeadingActions is not null)
        {
            builder.OpenElement(32, "div");
            builder.AddAttribute(33, "class", "sc-ai-input__leading-actions");
            builder.AddContent(34, LeadingActions);
            builder.CloseElement();
        }

        builder.OpenElement(40, "div");
        builder.AddAttribute(41, "class", "sc-ai-input__body");

        builder.OpenElement(42, "textarea");
        builder.AddAttribute(
            43,
            "class",
            string.IsNullOrWhiteSpace(TextAreaClass)
                ? "sc-ai-input__textarea"
                : $"sc-ai-input__textarea {TextAreaClass}");
        builder.AddAttribute(44, "placeholder", Placeholder ?? "Type a message...");
        builder.AddAttribute(45, "disabled", IsConversationBusy || IsComposing);
        builder.AddAttribute(46, "value", _text);
        builder.AddAttribute(47, "aria-label", Label ?? Placeholder ?? "Type a message...");
        builder.AddAttribute(
            48,
            "aria-describedby",
            CurrentMicrophonePermissionStatus is MicrophonePermissionStatus.None ||
                MicrophonePermissionContent is not null
                ? $"{_statusId} {_errorId}"
                : $"{_microphonePermissionId} {_statusId} {_errorId}");
        builder.AddAttribute(
            49,
            "oninput",
            EventCallback.Factory.Create<ChangeEventArgs>(
                this,
                eventArgs => SetText(eventArgs.Value?.ToString() ?? string.Empty)));
        builder.SetUpdatesAttributeName("value");
        builder.AddAttribute(
            50,
            "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(
                this,
                HandleTextAreaKeyDownAsync));
        builder.AddElementReferenceCapture(51, reference => _textArea = reference);
        builder.CloseElement();

        builder.CloseElement();

        if (TrailingActions is not null)
        {
            builder.OpenElement(65, "div");
            builder.AddAttribute(66, "class", "sc-ai-input__trailing-actions");
            builder.AddContent(67, TrailingActions);
            builder.CloseElement();
        }
        else if (CanCancel)
        {
            if (ShowDefaultStopButton)
            {
                builder.OpenComponent<MessageStopButton>(70);
                builder.AddComponentParameter(71, nameof(MessageStopButton.Label), StopLabel);
                builder.CloseComponent();
            }
        }
        else if (ShowDefaultSendButton)
        {
            builder.OpenComponent<MessageSendButton>(70);
            builder.AddComponentParameter(71, nameof(MessageSendButton.Label), SendLabel);
            builder.CloseComponent();
        }

        builder.CloseElement();

        if (BottomContent is not null)
        {
            builder.AddContent(80, BottomContent(_context));
        }

        builder.OpenElement(90, "div");
        builder.AddAttribute(91, "id", _statusId);
        builder.AddAttribute(92, "class", "sc-ai-input__status");
        builder.AddAttribute(93, "role", "status");
        builder.AddAttribute(94, "aria-live", "polite");
        builder.AddAttribute(95, "aria-atomic", "true");
        builder.AddContent(96, StatusMessage);
        builder.CloseElement();

        builder.OpenElement(100, "div");
        builder.AddAttribute(101, "id", _errorId);
        builder.AddAttribute(102, "class", "sc-ai-input__error");
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            builder.AddAttribute(103, "role", "alert");
            builder.AddContent(104, ErrorMessage);
        }
        builder.CloseElement();

        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (firstRender)
        {
            try
            {
                _keyboardCallbacksReference = DotNetObjectReference.Create(
                    new KeyboardCallbacks(this));
                _interop = new MessageInputInterop(JSRuntime);
                await _interop.InitializeAsync(
                    _textArea,
                    _keyboardCallbacksReference);
            }
            catch (JSException)
            {
                ErrorMessage =
                    "Keyboard shortcuts could not be initialized. Use the send button instead.";
                Refresh();
                return;
            }
        }

        if (_interop is not null && _keyboardBusy != CanCancel)
        {
            _keyboardBusy = CanCancel;
            await _interop.SetBusyAsync(CanCancel);
        }
    }

    private Task HandleTextAreaKeyDownAsync(KeyboardEventArgs eventArgs)
    {
        return eventArgs.Key == "Enter" &&
            !eventArgs.ShiftKey &&
            !eventArgs.IsComposing &&
            !eventArgs.Repeat &&
            CanSubmit
            ? SubmitAsync()
            : Task.CompletedTask;
    }

    private Task HandleEscapeAsync()
    {
        return InvokeAsync(async () =>
        {
            if (CanCancel)
            {
                await CancelAsync();
            }
        });
    }

    internal void SetText(string? value)
    {
        _text = value ?? string.Empty;
        ErrorMessage = null;
        Refresh();
    }

    internal ValueTask AddAttachmentAsync(DataContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _attachments.Add(content);
        ErrorMessage = null;
        Refresh();
        return ValueTask.CompletedTask;
    }

    internal async ValueTask RemoveAttachmentAsync(DataContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!_attachments.Remove(content))
        {
            return;
        }

        StatusMessage = $"{GetAttachmentName(content)} removed.";
        Refresh();
        await FocusAsync();
    }

    internal async Task SubmitAsync()
    {
        if (!CanSubmit)
        {
            return;
        }

        var text = _text.Trim();
        if (text.Length == 0)
        {
            text = AttachmentOnlyText;
        }

        var message = new ChatMessage(
            ChatRole.User,
            [new TextContent(text), .. _attachments]);

        _text = string.Empty;
        _attachments.Clear();
        ErrorMessage = null;
        StatusMessage = "Message sent.";
        Refresh();

        await OnSubmitted.InvokeAsync(message);
        await AgentContext.SendMessageAsync(message);
        await FocusAsync();
    }

    internal async Task CancelAsync()
    {
        if (!CanCancel)
        {
            return;
        }

        await AgentContext.CancelAsync();
        StatusMessage = "Response stopped.";
        await OnCanceled.InvokeAsync();
        await FocusAsync();
    }

    internal ValueTask FocusAsync()
    {
        return _textArea.Context is null
            ? ValueTask.CompletedTask
            : _textArea.FocusAsync();
    }

    internal void SetStatusMessage(string? message)
    {
        StatusMessage = message;
        if (!string.IsNullOrEmpty(message))
        {
            ErrorMessage = null;
        }
        Refresh();
    }

    internal void SetErrorMessage(string? message)
    {
        ErrorMessage = message;
        Refresh();
    }

    internal void SetMicrophonePermissionStatus(
        object owner,
        MicrophonePermissionStatus status)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _microphonePermissionOwner = owner;
        CurrentMicrophonePermissionStatus = status;
        Refresh();
    }

    internal void ClearMicrophonePermissionStatus(object? owner = null)
    {
        if (owner is not null && !ReferenceEquals(_microphonePermissionOwner, owner))
        {
            return;
        }

        _microphonePermissionOwner = null;
        CurrentMicrophonePermissionStatus = MicrophonePermissionStatus.None;
        Refresh();
    }

    internal void SetComposing(bool value)
    {
        _isComposing = value;
        Refresh();
    }

    private void Refresh()
    {
        StateHasChanged();
        _context.NotifyChanged();
    }

    private void RenderDefaultMicrophonePermission(RenderTreeBuilder builder)
    {
        var isRequesting =
            CurrentMicrophonePermissionStatus is MicrophonePermissionStatus.Requesting;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", _microphonePermissionId);
        builder.AddAttribute(
            2,
            "class",
            isRequesting
                ? "sc-ai-input__microphone-permission sc-ai-input__microphone-permission--requesting"
                : "sc-ai-input__microphone-permission sc-ai-input__microphone-permission--error");
        builder.AddAttribute(3, "role", isRequesting ? "status" : "alert");
        builder.AddAttribute(4, "aria-live", isRequesting ? "polite" : "assertive");
        builder.AddAttribute(5, "aria-atomic", "true");
        builder.AddContent(6, CurrentMicrophonePermissionStatus switch
        {
            MicrophonePermissionStatus.Requesting =>
                RequestingMicrophonePermissionMessage,
            MicrophonePermissionStatus.Denied =>
                MicrophonePermissionDeniedMessage,
            MicrophonePermissionStatus.Unavailable =>
                MicrophoneUnavailableMessage,
            _ => string.Empty,
        });
        builder.CloseElement();
    }

    private string CssClass()
    {
        var css = "sc-ai-input-container";
        if (AdditionalAttributes?.TryGetValue("class", out var value) == true &&
            value is string additionalClass)
        {
            css = $"{css} {additionalClass}";
        }

        return css;
    }

    private static string GetAttachmentName(DataContent content)
    {
        return string.IsNullOrWhiteSpace(content.Name)
            ? "Attachment"
            : content.Name;
    }

    /// <summary>
    /// Removes the status subscription this input registered on the conversation.
    /// </summary>
    public void Dispose()
    {
        _statusSubscription?.Dispose();
    }

    /// <summary>
    /// Removes subscriptions and releases browser keyboard handling resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Dispose();

        if (_interop is not null)
        {
            await _interop.DisposeAsync();
        }

        _keyboardCallbacksReference?.Dispose();
    }

    private sealed class KeyboardCallbacks(MessageInput owner)
    {
        [JSInvokable]
        public Task HandleEscapeAsync()
        {
            return owner.HandleEscapeAsync();
        }
    }
}
