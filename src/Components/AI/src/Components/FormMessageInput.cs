// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

[StreamRendering]
public class FormMessageInput : IComponent, IDisposable
{
    private RenderHandle _renderHandle;
    private AgentContext _agentContext = default!;
    private string? _placeholder;
    private IDisposable? _statusChangedSub;

    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    [Parameter]
    public string? Placeholder { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        _agentContext = AgentContext
            ?? throw new InvalidOperationException(
                "FormMessageInput must be inside an AgentFormBoundary.");
        _placeholder = Placeholder;

        _statusChangedSub ??= _agentContext.RegisterOnStatusChanged(_ => Render());

        Render();
        return Task.CompletedTask;
    }

    private void Render()
    {
        _renderHandle.Render(builder =>
        {
            var isDisabled = _agentContext.Status == ConversationStatus.Streaming;

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "sc-ai-input");

            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "class", "sc-ai-input__body");

            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "type", "text");
            builder.AddAttribute(6, "name", "UserMessage");
            builder.AddAttribute(7, "class", "sc-ai-input__textarea");
            builder.AddAttribute(8, "placeholder", _placeholder ?? "Type a message...");
            builder.AddAttribute(9, "disabled", isDisabled);
            builder.AddAttribute(10, "autocomplete", "off");
            builder.CloseElement(); // input

            builder.CloseElement(); // body div

            builder.OpenElement(20, "button");
            builder.AddAttribute(21, "type", "submit");
            builder.AddAttribute(22, "class", "sc-ai-input__send");
            builder.AddAttribute(23, "disabled", isDisabled);
            builder.AddAttribute(24, "aria-label", "Send message");
            builder.AddMarkupContent(25,
                "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M22 2 11 13\"/><path d=\"M22 2 15 22 11 13 2 9z\"/></svg>");
            builder.CloseElement(); // button

            builder.CloseElement(); // outer div
        });
    }

    public void Dispose()
    {
        _statusChangedSub?.Dispose();
    }
}
