// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public sealed class SuggestionList : IComponent, IDisposable
{
    private RenderHandle _renderHandle;
    private AgentContext _agentContext = default!;
    private IReadOnlyList<Suggestion> _suggestions = [];
    private bool _isDisabled;
    private IDisposable? _statusSub;

    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<Suggestion> Suggestions { get; set; } = [];

    [Parameter]
    public RenderFragment<Suggestion>? ItemTemplate { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        _agentContext = AgentContext
            ?? throw new InvalidOperationException(
                "SuggestionList must be inside an AgentBoundary.");
        _suggestions = Suggestions;

        _statusSub = _agentContext.RegisterOnStatusChanged(status =>
        {
            _isDisabled = status == ConversationStatus.Streaming;
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
            builder.AddAttribute(1, "class", "sc-ai-suggestions");
            builder.AddAttribute(2, "role", "group");
            builder.AddAttribute(3, "aria-label", "Suggestions");

            var seq = 10;
            foreach (var suggestion in _suggestions)
            {
                var prompt = suggestion.Prompt;
                builder.OpenElement(seq, "button");
                builder.AddAttribute(seq + 1, "class", "sc-ai-suggestions__chip");
                builder.AddAttribute(seq + 2, "type", "button");
                builder.AddAttribute(seq + 3, "disabled", _isDisabled);
                builder.AddAttribute(seq + 4, "onclick",
                    EventCallback.Factory.Create(this,
                        () => _agentContext.SendMessageAsync(prompt)));

                if (ItemTemplate is not null)
                {
                    builder.AddContent(seq + 5, ItemTemplate(suggestion));
                }
                else
                {
                    builder.AddContent(seq + 5, suggestion.Label);
                }

                builder.CloseElement(); // button
                seq += 10;
            }

            builder.CloseElement(); // div
        });
    }

    public void Dispose()
    {
        _statusSub?.Dispose();
    }
}
