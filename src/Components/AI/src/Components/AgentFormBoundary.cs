// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

[StreamRendering]
public class AgentFormBoundary : ComponentBase, IDisposable
{
    public const string FormHandlerName = "ai-chat";

    private readonly Func<Task> _handleSubmitDelegate;
    private AgentContext _context = default!;

    public AgentFormBoundary()
    {
        _handleSubmitDelegate = HandleSubmitAsync;
    }

    [Parameter, EditorRequired]
    public UIAgent Agent { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [SupplyParameterFromForm(FormName = FormHandlerName)]
    internal string? UserMessage { get; set; }

    [SupplyParameterFromForm(FormName = FormHandlerName)]
    internal string? BlockAction { get; set; }

    protected override void OnInitialized()
    {
        _context = new AgentContext(Agent);
    }

    protected override async Task OnInitializedAsync()
    {
        var thread = Agent.Options.Thread;
        if (thread is not null && thread.GetUpdates().Count > 0)
        {
            await _context.RestoreAsync();
        }
    }

    private async Task HandleSubmitAsync()
    {
        if (!string.IsNullOrEmpty(BlockAction))
        {
            await ProcessBlockActionAsync();
        }
        else if (!string.IsNullOrEmpty(UserMessage))
        {
            await _context.SendMessageAsync(UserMessage);
            UserMessage = null;
        }
    }

    private async Task ProcessBlockActionAsync()
    {
        var separatorIndex = BlockAction!.IndexOf(':');
        if (separatorIndex < 0)
        {
            return;
        }

        var action = BlockAction.AsSpan(0, separatorIndex);
        var blockId = BlockAction.AsSpan(separatorIndex + 1);
        var approved = action.Equals("approve", StringComparison.OrdinalIgnoreCase);

        var thread = Agent.Options.Thread;
        if (thread is null)
        {
            return;
        }

        // Find the pending approval request in stored updates by matching CallId.
        ToolApprovalRequestContent? request = null;
        foreach (var update in thread.GetUpdates())
        {
            foreach (var content in update.Contents)
            {
                if (content is ToolApprovalRequestContent tar
                    && tar.ToolCall is FunctionCallContent fcc
                    && blockId.Equals(fcc.CallId, StringComparison.Ordinal))
                {
                    request = tar;
                    break;
                }
            }

            if (request is not null)
            {
                break;
            }
        }

        if (request is null)
        {
            return;
        }

        var response = request.CreateResponse(approved);
        var message = new ChatMessage(ChatRole.User, [response]);
        await _context.SendMessageAsync(message);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<AgentContext>>(0);
        builder.AddComponentParameter(1, "Value", _context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(10, "form");
            inner.AddAttribute(11, "method", "post");
            inner.AddAttribute(12, "data-enhance", true);
            inner.AddAttribute(13, "onsubmit", _handleSubmitDelegate);
            inner.AddNamedEvent("onsubmit", FormHandlerName);

            inner.OpenComponent<AntiforgeryToken>(15);
            inner.CloseComponent();

            if (Agent.Options.Thread is not null)
            {
                inner.OpenElement(20, "input");
                inner.AddAttribute(21, "type", "hidden");
                inner.AddAttribute(22, "name", "ThreadId");
                inner.AddAttribute(23, "value", Agent.Options.Thread.ThreadId);
                inner.CloseElement();
            }

            inner.AddContent(30, ChildContent);

            inner.CloseElement(); // form
        }));
        builder.CloseComponent();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
