// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI;

public class MessageListContext
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly List<BlockRendererRegistration> _registrations = new();

    public RenderFragment RenderBlock(ContentBlock block)
    {
        foreach (var reg in _registrations)
        {
            if (reg.BlockType.IsAssignableFrom(block.GetType())
                && (reg.When is null || reg.When(block)))
            {
                return reg.Render(block);
            }
        }

        return builder =>
        {
            if (block is RichContentBlock rich)
            {
                var role = block.Role == Microsoft.Extensions.AI.ChatRole.User ? "user" : "assistant";
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", $"sc-ai-message sc-ai-message--{role}");
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "sc-ai-message__bubble");
                builder.OpenElement(4, "div");
                var contentClass = block.LifecycleState == BlockLifecycleState.Active
                    ? "sc-ai-message__content sc-ai-message__content--streaming"
                    : "sc-ai-message__content";
                builder.AddAttribute(5, "class", contentClass);
                builder.AddContent(6, rich.RawText);
                builder.CloseElement(); // content div
                builder.CloseElement(); // bubble div
                builder.CloseElement(); // message div
            }
            else if (block is FunctionApprovalBlock approval)
            {
                RenderApprovalBlock(builder, approval);
            }
            else if (block is FunctionInvocationContentBlock)
            {
                // Not rendered by default. Register a BlockRenderer<FunctionInvocationContentBlock>
                // to display tool call blocks.
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "sc-ai-tool-call");
                builder.AddContent(2, block.GetType().Name);
                builder.CloseElement();
            }
        };
    }

    internal Action? OnRegistrationsChanged { get; set; }

    internal void AddRegistration(BlockRendererRegistration registration)
    {
        _registrations.Add(registration);
        OnRegistrationsChanged?.Invoke();
    }

    internal void RemoveRegistration(BlockRendererRegistration registration)
    {
        _registrations.Remove(registration);
        OnRegistrationsChanged?.Invoke();
    }

    private static void RenderApprovalBlock(RenderTreeBuilder builder, FunctionApprovalBlock block)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sc-ai-approval");

        // Header
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "sc-ai-approval__header");
        builder.AddContent(4, "\u26a0\ufe0f Approval Required");
        builder.CloseElement();

        // Tool name
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "class", "sc-ai-approval__tool-name");
        builder.AddContent(7, block.ToolName ?? "unknown");
        builder.CloseElement();

        // Arguments
        if (block.Arguments is { Count: > 0 })
        {
            builder.OpenElement(10, "pre");
            builder.AddAttribute(11, "class", "sc-ai-tool-call__pre");
            builder.AddContent(12, JsonSerializer.Serialize(block.Arguments, IndentedJsonOptions));
            builder.CloseElement();
        }

        // Actions or status
        if (block.Status == ApprovalStatus.Pending)
        {
            builder.OpenElement(20, "div");
            builder.AddAttribute(21, "class", "sc-ai-approval__actions");

            builder.OpenElement(22, "button");
            builder.AddAttribute(23, "type", "button");
            builder.AddAttribute(26, "class", "sc-ai-btn sc-ai-btn--primary");
            builder.AddAttribute(27, "onclick", (Action)block.Approve);
            builder.AddContent(28, "Approve");
            builder.CloseElement();

            builder.OpenElement(30, "button");
            builder.AddAttribute(31, "type", "button");
            builder.AddAttribute(34, "class", "sc-ai-btn sc-ai-btn--secondary");
            builder.AddAttribute(35, "onclick", (Action)(() => block.Reject()));
            builder.AddContent(36, "Reject");
            builder.CloseElement();

            builder.CloseElement(); // actions
        }
        else
        {
            var statusClass = block.Status == ApprovalStatus.Approved
                ? "sc-ai-approval__status sc-ai-approval__status--approved"
                : "sc-ai-approval__status sc-ai-approval__status--rejected";
            var statusText = block.Status == ApprovalStatus.Approved ? "\u2713 Approved" : "\u2717 Rejected";

            builder.OpenElement(30, "div");
            builder.AddAttribute(31, "class", statusClass);
            builder.AddContent(32, statusText);
            builder.CloseElement();
        }

        builder.CloseElement(); // sc-ai-approval
    }
}
