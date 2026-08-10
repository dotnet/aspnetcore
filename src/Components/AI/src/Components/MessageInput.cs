// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Text input that sends messages to the cascaded <see cref="AgentContext"/> and disables
/// itself while a response streams.
/// </summary>
public class MessageInput : IComponent, IDisposable
{
    private RenderHandle _renderHandle;
    private AgentContext _agentContext = default!;
    private string? _placeholder;
    private RenderFragment? _leadingActions;
    private RenderFragment? _trailingActions;
    private string _text = "";
    private bool _isDisabled;
    private IDisposable? _statusSub;

    /// <summary>
    /// Gets or sets the conversation this input sends messages to.
    /// </summary>
    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the placeholder text of the input.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the content rendered before the text area.
    /// </summary>
    [Parameter]
    public RenderFragment? LeadingActions { get; set; }

    /// <summary>
    /// Gets or sets the content rendered after the text area. Replaces the default send button.
    /// </summary>
    [Parameter]
    public RenderFragment? TrailingActions { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        _agentContext = AgentContext
            ?? throw new InvalidOperationException(
                "MessageInput must be inside an AgentBoundary.");
        _placeholder = Placeholder;
        _leadingActions = LeadingActions;
        _trailingActions = TrailingActions;

        // Register once. The AgentContext cascade is fixed for the lifetime of the component,
        // so re-registering on every parameter set would accumulate handlers and retain the
        // component on each parent re-render.
        _statusSub ??= _agentContext.RegisterOnStatusChanged(status =>
        {
            _isDisabled = status is ConversationStatus.Streaming or ConversationStatus.AwaitingInput;
            Render();
        });

        Render();
        return Task.CompletedTask;
    }

    private void Render()
    {
        _renderHandle.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "sc-ai-input");

            if (_leadingActions is not null)
            {
                builder.AddContent(2, _leadingActions);
            }

            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "class", "sc-ai-input__body");

            builder.OpenElement(10, "textarea");
            builder.AddAttribute(11, "class", "sc-ai-input__textarea");
            builder.AddAttribute(12, "placeholder", _placeholder ?? "Type a message...");
            builder.AddAttribute(13, "disabled", _isDisabled);
            builder.AddAttribute(14, "value", _text);
            builder.AddAttribute(15, "aria-label", _placeholder ?? "Type a message...");
            builder.AddAttribute(16, "oninput",
                EventCallback.Factory.Create<ChangeEventArgs>(
                    this, e =>
                    {
                        _text = e.Value?.ToString() ?? "";
                        // Re-render so the component's rendered value tracks the DOM value.
                        // Without this, clearing _text on submit produces no diff (the last
                        // rendered value was already empty) and the textarea is never cleared.
                        Render();
                    }));
            builder.AddAttribute(17, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, OnKeyDown));
            builder.CloseElement(); // textarea

            builder.CloseElement(); // input body

            if (_trailingActions is not null)
            {
                builder.AddContent(50, _trailingActions);
            }
            else
            {
                builder.OpenElement(50, "button");
                builder.AddAttribute(51, "type", "button");
                builder.AddAttribute(52, "class", "sc-ai-input__send");
                builder.AddAttribute(53, "disabled", _isDisabled);
                builder.AddAttribute(54, "aria-label", "Send message");
                builder.AddAttribute(55, "onclick",
                    EventCallback.Factory.Create(this, SubmitAsync));

                // Send icon SVG
                builder.AddMarkupContent(56,
                    "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M22 2 11 13\"/><path d=\"M22 2 15 22 11 13 2 9z\"/></svg>");

                builder.CloseElement();
            }

            builder.CloseElement(); // div
        });
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !_isDisabled)
        {
            await SubmitAsync();
        }
    }

    private async Task SubmitAsync()
    {
        if (_isDisabled || string.IsNullOrWhiteSpace(_text))
        {
            return;
        }

        var text = _text;
        _text = "";
        Render();

        await _agentContext.SendMessageAsync(new ChatMessage(ChatRole.User, text));
    }

    /// <summary>
    /// Removes the status subscription this input registered on the conversation.
    /// </summary>
    public void Dispose()
    {
        _statusSub?.Dispose();
        GC.SuppressFinalize(this);
    }
}
