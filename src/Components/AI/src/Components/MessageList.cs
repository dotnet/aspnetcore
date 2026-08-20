// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Renders the turns of the cascaded <see cref="AgentContext"/> and updates them as blocks
/// stream in.
/// </summary>
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
    private ConversationStatus _previousStatus;
    private string? _announcement;

    /// <summary>
    /// Gets or sets the conversation rendered by this list.
    /// </summary>
    [CascadingParameter]
    public AgentContext AgentContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the content rendered above the turns. Use it to register
    /// <see cref="BlockRenderer{TBlock}"/> components.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the content rendered when the conversation has no turns.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Gets or sets the content rendered below the turns. Defaults to the streaming and error
    /// indicators.
    /// </summary>
    [Parameter]
    public RenderFragment<AgentContext>? Footer { get; set; }

    /// <summary>
    /// Gets or sets the accessible label for the conversation transcript.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Conversation";

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
            _previousStatus = _agentContext.Status;
            _statusChangedSub = _agentContext.RegisterOnStatusChanged(OnStatusChanged);

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
                    inner.AddAttribute(4, "role", "log");
                    inner.AddAttribute(5, "aria-label", Label);
                    inner.AddAttribute(6, "aria-live", "off");
                    inner.AddAttribute(7, "aria-relevant", "additions");
                    inner.AddAttribute(
                        8,
                        "aria-busy",
                        _agentContext.Status == ConversationStatus.Streaming ? "true" : "false");
                    if (_childContent is not null)
                    {
                        inner.AddContent(10, _childContent);
                    }

                    if (_turnRenderers.Count == 0 && EmptyContent is not null)
                    {
                        inner.AddContent(11, EmptyContent);
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

                    inner.OpenElement(seq + 20, "div");
                    inner.AddAttribute(seq + 21, "class", "sc-ai-sr-only");
                    inner.AddAttribute(seq + 22, "role", "status");
                    inner.AddAttribute(seq + 23, "aria-live", "polite");
                    inner.AddAttribute(seq + 24, "aria-atomic", "true");
                    inner.AddContent(seq + 25, _announcement);
                    inner.CloseElement();

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
                builder.AddAttribute(seq + 2, "aria-hidden", "true");
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
                builder.AddContent(seq + 5, "Something went wrong. Please try again.");
                builder.CloseElement(); // span
                builder.OpenElement(seq + 6, "button");
                builder.AddAttribute(seq + 7, "type", "button");
                builder.AddAttribute(seq + 8, "class", "sc-ai-btn sc-ai-btn--secondary");
                builder.AddAttribute(seq + 9, "onclick",
                    EventCallback.Factory.Create(this,
                        () => _agentContext.RetryAsync()));
                builder.AddContent(seq + 10, "Retry");
                builder.CloseElement(); // button
                builder.CloseElement(); // div
                break;
        }
    }

    private void OnStatusChanged(ConversationStatus status)
    {
        _announcement = status switch
        {
            ConversationStatus.Streaming => "Assistant is responding.",
            ConversationStatus.AwaitingInput => "Assistant is waiting for your input.",
            ConversationStatus.Error => "The response failed.",
            ConversationStatus.Idle when _previousStatus is
                ConversationStatus.Streaming or ConversationStatus.AwaitingInput =>
                "Response complete.",
            _ => _announcement,
        };
        _previousStatus = status;
        Render();
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

    /// <summary>
    /// Removes the subscriptions this list registered on the conversation.
    /// </summary>
    public void Dispose()
    {
        ResetRegistrations();
        GC.SuppressFinalize(this);
    }
}
