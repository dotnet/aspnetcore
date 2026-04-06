// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

[StreamRendering]
public class MessageList : IComponent, IDisposable
{
    private RenderHandle _renderHandle;
    private AgentContext _agentContext = default!;
    private RenderFragment? _childContent;
    private RenderFragment<AgentContext>? _footer;
    private readonly MessageListContext _listContext = new();
    private readonly List<ConversationTurnRenderer> _turnRenderers = new();
    private IDisposable? _turnAddedSub;
    private IDisposable? _statusChangedSub;

    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment<AgentContext>? Footer { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        var newAgentContext = AgentContext
            ?? throw new InvalidOperationException(
                "MessageList must be inside an AgentBoundary.");

        if (!ReferenceEquals(_agentContext, newAgentContext))
        {
            ResetRegistrations();
            _agentContext = newAgentContext;
            _listContext.OnRegistrationsChanged = Render;

            _turnAddedSub = _agentContext.RegisterOnTurnAdded(OnTurnAdded);
            _statusChangedSub = _agentContext.RegisterOnStatusChanged(_ => Render());

            foreach (var turn in _agentContext.Turns)
            {
                var renderer = new ConversationTurnRenderer(
                    _agentContext, turn, _listContext, Render);
                _turnRenderers.Add(renderer);
            }
        }

        _childContent = ChildContent;
        _footer = Footer;

        Render();
        return Task.CompletedTask;
    }

    private void OnTurnAdded(ConversationTurn turn)
    {
        var renderer = new ConversationTurnRenderer(
            _agentContext, turn, _listContext, Render);
        _turnRenderers.Add(renderer);
        Render();
    }

    private void Render()
    {
        _renderHandle.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<MessageListContext>>(0);
            builder.AddComponentParameter(1, "Value", _listContext);
            builder.AddComponentParameter(2, "IsFixed", true);
            builder.AddComponentParameter(3, "ChildContent",
                (RenderFragment)(inner =>
                {
                    inner.OpenElement(3, "div");
                    inner.AddAttribute(3, "class", "sc-ai-message-list");
                    if (_childContent is not null)
                    {
                        inner.AddContent(4, _childContent);
                    }

                    var seq = 100;
                    foreach (var turnRenderer in _turnRenderers)
                    {
                        turnRenderer.RenderTo(inner, seq);
                        seq += 100;
                    }

                    inner.OpenElement(seq, "div");
                    inner.AddAttribute(seq + 1, "class", "sc-ai-message-list__footer");
                    if (_footer is not null)
                    {
                        inner.AddContent(seq + 2, _footer(_agentContext));
                    }
                    else
                    {
                        RenderDefaultFooter(inner, seq + 2);
                    }
                    inner.CloseElement(); // footer div

                    inner.CloseElement(); // sc-ai-message-list div
                }));
            builder.CloseComponent();
        });
    }

    private void RenderDefaultFooter(RenderTreeBuilder builder, int seq)
    {
        switch (_agentContext.Status)
        {
            case ConversationStatus.Streaming:
                builder.OpenElement(seq, "div");
                builder.AddAttribute(seq + 1, "class", "sc-ai-typing");
                builder.AddAttribute(seq + 2, "role", "status");
                builder.AddAttribute(seq + 3, "aria-label", "Agent is typing");
                for (var i = 0; i < 3; i++)
                {
                    builder.OpenElement(seq + 4 + i, "span");
                    builder.AddAttribute(seq + 7 + i, "class", "sc-ai-typing__dot");
                    builder.CloseElement();
                }
                builder.CloseElement();
                break;

            case ConversationStatus.Error:
                builder.OpenElement(seq, "div");
                builder.AddAttribute(seq + 1, "class", "sc-ai-error");
                builder.AddAttribute(seq + 2, "role", "alert");
                builder.OpenElement(seq + 3, "span");
                builder.AddAttribute(seq + 4, "class", "sc-ai-error__message");
                builder.AddContent(seq + 5, _agentContext.Error?.Message ?? "Something went wrong.");
                builder.CloseElement(); // span
                builder.OpenElement(seq + 6, "button");
                builder.AddAttribute(seq + 7, "class", "sc-ai-btn sc-ai-btn--secondary");
                builder.AddAttribute(seq + 8, "onclick",
                    EventCallback.Factory.Create(this,
                        () => _agentContext.RetryAsync()));
                builder.AddContent(seq + 9, "Retry");
                builder.CloseElement(); // button
                builder.CloseElement(); // div
                break;
        }
    }

    private void ResetRegistrations()
    {
        _turnAddedSub?.Dispose();
        _turnAddedSub = null;

        _statusChangedSub?.Dispose();
        _statusChangedSub = null;

        foreach (var renderer in _turnRenderers)
        {
            renderer.Dispose();
        }

        _turnRenderers.Clear();
    }

    public void Dispose()
    {
        ResetRegistrations();
    }
}
