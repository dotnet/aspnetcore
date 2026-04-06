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
            else if (block is ReasoningContentBlock reasoning)
            {
                RenderReasoningBlock(builder, reasoning);
            }
            else if (block is MediaContentBlock media)
            {
                RenderMediaBlock(builder, media);
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

    private static void RenderReasoningBlock(RenderTreeBuilder builder, ReasoningContentBlock block)
    {
        var isStreaming = block.LifecycleState == BlockLifecycleState.Active;

        builder.OpenElement(0, "details");
        builder.AddAttribute(1, "class", "sc-ai-reasoning");
        if (isStreaming)
        {
            builder.AddAttribute(2, "open", true);
        }

        builder.OpenElement(3, "summary");
        builder.AddAttribute(4, "class", "sc-ai-reasoning__header");
        if (isStreaming)
        {
            builder.AddContent(5, "\ud83d\udca1 Thinking\u2026");
        }
        else
        {
            builder.AddContent(5, "\ud83d\udca1 Thought process");
        }
        builder.CloseElement(); // summary

        builder.OpenElement(6, "div");
        builder.AddAttribute(7, "class", "sc-ai-reasoning__content");
        builder.AddContent(8, block.Text);
        builder.CloseElement(); // content div

        builder.CloseElement(); // details
    }

    private static void RenderMediaBlock(RenderTreeBuilder builder, MediaContentBlock block)
    {
        var role = block.Role == Microsoft.Extensions.AI.ChatRole.User ? "user" : "assistant";

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"sc-ai-message sc-ai-message--{role}");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "sc-ai-media");

        foreach (var item in block.Items)
        {
            var mediaType = item.MediaType ?? "application/octet-stream";
            if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var src = GetDataContentUri(item);
                builder.OpenElement(4, "img");
                builder.AddAttribute(5, "src", src);
                builder.AddAttribute(6, "alt", "Attached image");
                builder.AddAttribute(7, "class", "sc-ai-media__image");
                builder.CloseElement();
            }
            else if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                var src = GetDataContentUri(item);
                builder.OpenElement(4, "audio");
                builder.AddAttribute(5, "controls", true);
                builder.AddAttribute(6, "class", "sc-ai-media__audio");
                builder.OpenElement(7, "source");
                builder.AddAttribute(8, "src", src);
                builder.AddAttribute(9, "type", mediaType);
                builder.CloseElement();
                builder.CloseElement();
            }
            else if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                var src = GetDataContentUri(item);
                builder.OpenElement(4, "video");
                builder.AddAttribute(5, "controls", true);
                builder.AddAttribute(6, "class", "sc-ai-media__video");
                builder.OpenElement(7, "source");
                builder.AddAttribute(8, "src", src);
                builder.AddAttribute(9, "type", mediaType);
                builder.CloseElement();
                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(4, "div");
                builder.AddAttribute(5, "class", "sc-ai-media__file");
                builder.AddContent(6, $"\ud83d\udcce {mediaType}");
                builder.CloseElement();
            }
        }

        builder.CloseElement(); // sc-ai-media
        builder.CloseElement(); // message
    }

    private static string GetDataContentUri(Microsoft.Extensions.AI.DataContent content)
    {
        if (content.Uri is not null && content.Uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return content.Uri;
        }

        if (content.Data is { Length: > 0 } data)
        {
            var mediaType = content.MediaType ?? "application/octet-stream";
            return $"data:{mediaType};base64,{Convert.ToBase64String(data.ToArray())}";
        }

        return content.Uri ?? "";
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

            // type="submit" + name/value enables form-based approval in SSR mode.
            // onclick enables interactive approval in Server/WebAssembly mode.
            // Both attributes coexist safely: in interactive mode there is no parent
            // form so submit is a no-op; in SSR mode there is no circuit so onclick
            // is ignored.
            builder.OpenElement(22, "button");
            builder.AddAttribute(23, "type", "submit");
            builder.AddAttribute(24, "name", "BlockAction");
            builder.AddAttribute(25, "value", $"approve:{block.Id}");
            builder.AddAttribute(26, "class", "sc-ai-btn sc-ai-btn--primary");
            builder.AddAttribute(27, "onclick", (Action)block.Approve);
            builder.AddContent(28, "Approve");
            builder.CloseElement();

            builder.OpenElement(30, "button");
            builder.AddAttribute(31, "type", "submit");
            builder.AddAttribute(32, "name", "BlockAction");
            builder.AddAttribute(33, "value", $"reject:{block.Id}");
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
