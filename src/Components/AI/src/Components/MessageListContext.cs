// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Resolves the markup used to render a block inside a <see cref="MessageList"/>. Renderers
/// registered with <see cref="BlockRenderer{TBlock}"/> take precedence over the built-in
/// rendering.
/// </summary>
public class MessageListContext
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly List<BlockRendererRegistration> _registrations = new();

    /// <summary>
    /// Returns the markup for a block.
    /// </summary>
    /// <param name="block">The block to render.</param>
    /// <returns>The markup that renders <paramref name="block"/>.</returns>
    public RenderFragment RenderBlock(ContentBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        for (var i = _registrations.Count - 1; i >= 0; i--)
        {
            var reg = _registrations[i];
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
                var role = block.Role == ChatRole.User ? "user" : "assistant";
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", $"sc-ai-message sc-ai-message--{role}");
                builder.OpenElement(2, "div");
                builder.AddAttribute(3, "class", "sc-ai-message__bubble");
                builder.OpenElement(4, "div");
                var contentClass = block.LifecycleState == BlockLifecycleState.Active
                    ? "sc-ai-message__content sc-ai-message__content--streaming"
                    : "sc-ai-message__content";
                builder.AddAttribute(5, "class", contentClass);
                if (rich.Content.Count > 0)
                {
                    RenderRichTextNodes(builder, rich.Content);
                }
                else
                {
                    builder.AddContent(6, rich.RawText);
                }
                builder.CloseElement(); // content div
                builder.CloseElement(); // bubble div
                builder.CloseElement(); // message div
            }
            else if (block is FunctionApprovalBlock approval)
            {
                RenderApprovalBlock(builder, approval);
            }
            else if (block is not FunctionInvocationContentBlock)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "sc-ai-unknown-block");
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

    private static void RenderRichTextNodes(
        RenderTreeBuilder builder,
        IReadOnlyList<RichTextNode> nodes)
    {
        foreach (var node in nodes)
        {
            builder.OpenRegion(0);
            RenderRichTextNode(builder, node);
            builder.CloseRegion();
        }
    }

    private static void RenderRichTextNode(RenderTreeBuilder builder, RichTextNode node)
    {
        switch (node)
        {
            case TextNode text:
                builder.AddContent(0, text.Text);
                break;
            case ParagraphNode:
                RenderElement(builder, "p", "sc-ai-rich-text__paragraph", node.Children);
                break;
            case HeadingNode heading:
                RenderElement(
                    builder,
                    $"h{Math.Clamp(heading.Level, 1, 6)}",
                    "sc-ai-rich-text__heading",
                    node.Children);
                break;
            case EmphasisNode:
                RenderElement(builder, "em", null, node.Children);
                break;
            case StrongNode:
                RenderElement(builder, "strong", null, node.Children);
                break;
            case StrikethroughNode:
                RenderElement(builder, "s", null, node.Children);
                break;
            case InlineCodeNode inlineCode:
                builder.OpenElement(0, "code");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__inline-code");
                builder.AddContent(2, inlineCode.Code);
                builder.CloseElement();
                break;
            case CodeBlockNode codeBlock:
                builder.OpenElement(0, "pre");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__code-block");
                builder.OpenElement(2, "code");
                if (!string.IsNullOrEmpty(codeBlock.Language))
                {
                    builder.AddAttribute(3, "data-language", codeBlock.Language);
                }
                builder.AddContent(4, codeBlock.Code);
                builder.CloseElement();
                builder.CloseElement();
                break;
            case BlockQuoteNode:
                RenderElement(builder, "blockquote", "sc-ai-rich-text__quote", node.Children);
                break;
            case LineBreakNode:
                builder.OpenElement(0, "br");
                builder.CloseElement();
                break;
            case ThematicBreakNode:
                builder.OpenElement(0, "hr");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__thematic-break");
                builder.CloseElement();
                break;
            case ListNode list:
                builder.OpenElement(0, list.Ordered ? "ol" : "ul");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__list");
                if (list.Ordered && list.Start is not null)
                {
                    builder.AddAttribute(2, "start", list.Start.Value);
                }
                RenderRichTextNodes(builder, list.Children);
                builder.CloseElement();
                break;
            case ListItemNode item:
                builder.OpenElement(0, "li");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__list-item");
                if (item.Checked is not null)
                {
                    builder.OpenElement(2, "input");
                    builder.AddAttribute(3, "type", "checkbox");
                    builder.AddAttribute(4, "disabled", true);
                    if (item.Checked.Value)
                    {
                        builder.AddAttribute(5, "checked", true);
                    }
                    builder.CloseElement();
                }
                RenderRichTextNodes(builder, item.Children);
                builder.CloseElement();
                break;
            case LinkNode link:
                RenderLink(builder, link);
                break;
            case ImageNode image:
                RenderImage(builder, image);
                break;
            case DefinitionNode definition:
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__definition");
                builder.AddContent(2, $"[{definition.Label}]: {definition.Url}");
                builder.CloseElement();
                break;
            case LinkReferenceNode linkReference:
                RenderReference(builder, linkReference.Label, linkReference.Children);
                break;
            case ImageReferenceNode imageReference:
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__image-reference");
                builder.AddContent(2, imageReference.Alt ?? imageReference.Label);
                builder.CloseElement();
                break;
            case FootnoteNode:
                RenderElement(builder, "aside", "sc-ai-rich-text__footnote", node.Children);
                break;
            case FootnoteReferenceNode footnoteReference:
                builder.OpenElement(0, "sup");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__footnote-reference");
                builder.AddContent(2, footnoteReference.Label);
                builder.CloseElement();
                break;
            case FootnoteDefinitionNode footnoteDefinition:
                builder.OpenElement(0, "aside");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__footnote-definition");
                builder.AddAttribute(2, "data-label", footnoteDefinition.Label);
                RenderRichTextNodes(builder, footnoteDefinition.Children);
                builder.CloseElement();
                break;
            case HtmlNode html:
                builder.OpenElement(0, "code");
                builder.AddAttribute(1, "class", "sc-ai-rich-text__html");
                builder.AddContent(2, html.Value);
                builder.CloseElement();
                break;
            case TableNode table:
                RenderTable(builder, table);
                break;
            case TableRowNode:
            case TableCellNode:
                RenderRichTextNodes(builder, node.Children);
                break;
            default:
                RenderRichTextNodes(builder, node.Children);
                break;
        }
    }

    private static void RenderElement(
        RenderTreeBuilder builder,
        string elementName,
        string? className,
        IReadOnlyList<RichTextNode> children)
    {
        builder.OpenElement(0, elementName);
        if (className is not null)
        {
            builder.AddAttribute(1, "class", className);
        }
        RenderRichTextNodes(builder, children);
        builder.CloseElement();
    }

    private static void RenderLink(RenderTreeBuilder builder, LinkNode link)
    {
        var safeUrl = GetSafeUrl(link.Url, allowMailTo: true);
        if (safeUrl is null)
        {
            RenderRichTextNodes(builder, link.Children);
            return;
        }

        builder.OpenElement(0, "a");
        builder.AddAttribute(1, "href", safeUrl);
        if (link.Title is not null)
        {
            builder.AddAttribute(2, "title", link.Title);
        }
        RenderRichTextNodes(builder, link.Children);
        builder.CloseElement();
    }

    private static void RenderImage(RenderTreeBuilder builder, ImageNode image)
    {
        var safeUrl = GetSafeUrl(image.Url, allowMailTo: false);
        if (safeUrl is null)
        {
            builder.AddContent(0, image.Alt);
            return;
        }

        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "src", safeUrl);
        builder.AddAttribute(2, "alt", image.Alt ?? string.Empty);
        if (image.Title is not null)
        {
            builder.AddAttribute(3, "title", image.Title);
        }
        builder.CloseElement();
    }

    private static void RenderReference(
        RenderTreeBuilder builder,
        string label,
        IReadOnlyList<RichTextNode> children)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "sc-ai-rich-text__link-reference");
        if (children.Count > 0)
        {
            RenderRichTextNodes(builder, children);
        }
        else
        {
            builder.AddContent(2, label);
        }
        builder.CloseElement();
    }

    private static void RenderTable(RenderTreeBuilder builder, TableNode table)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sc-ai-rich-text__table-container");
        builder.OpenElement(2, "table");
        builder.AddAttribute(3, "class", "sc-ai-rich-text__table");
        builder.OpenElement(4, "tbody");

        foreach (var child in table.Children)
        {
            if (child is not TableRowNode row)
            {
                continue;
            }

            builder.OpenRegion(5);
            builder.OpenElement(0, "tr");
            var columnIndex = 0;
            foreach (var rowChild in row.Children)
            {
                if (rowChild is not TableCellNode cell)
                {
                    continue;
                }

                builder.OpenRegion(1);
                builder.OpenElement(0, "td");
                if (columnIndex < table.Alignment.Count &&
                    table.Alignment[columnIndex] != TableColumnAlignment.None)
                {
                    builder.AddAttribute(
                        1,
                        "class",
                        $"sc-ai-rich-text__table-cell--{table.Alignment[columnIndex].ToString().ToLowerInvariant()}");
                }
                RenderRichTextNodes(builder, cell.Children);
                builder.CloseElement();
                builder.CloseRegion();
                columnIndex++;
            }
            builder.CloseElement();
            builder.CloseRegion();
        }

        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private static string? GetSafeUrl(string url, bool allowMailTo)
    {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            return null;
        }

        if (!uri.IsAbsoluteUri)
        {
            return url;
        }

        if (uri.Scheme is "http" or "https" ||
            (allowMailTo && uri.Scheme == "mailto"))
        {
            return url;
        }

        return null;
    }

    private static void RenderApprovalBlock(
        RenderTreeBuilder builder,
        FunctionApprovalBlock block)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sc-ai-approval");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "sc-ai-approval__header");
        builder.AddContent(4, "Approval required");
        builder.CloseElement();

        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "class", "sc-ai-approval__tool-name");
        builder.AddContent(7, block.ToolName ?? "Unknown tool");
        builder.CloseElement();

        if (block.Arguments is { Count: > 0 })
        {
            builder.OpenElement(8, "pre");
            builder.AddAttribute(9, "class", "sc-ai-approval__arguments");
            builder.AddContent(10, JsonSerializer.Serialize(block.Arguments, IndentedJsonOptions));
            builder.CloseElement();
        }

        if (block.Status == ApprovalStatus.Pending)
        {
            builder.OpenElement(11, "div");
            builder.AddAttribute(12, "class", "sc-ai-approval__actions");

            builder.OpenElement(13, "button");
            builder.AddAttribute(14, "type", "button");
            builder.AddAttribute(15, "class", "sc-ai-btn sc-ai-btn--primary");
            builder.AddAttribute(16, "onclick", (Action)block.Approve);
            builder.AddContent(17, "Approve");
            builder.CloseElement();

            builder.OpenElement(18, "button");
            builder.AddAttribute(19, "type", "button");
            builder.AddAttribute(20, "class", "sc-ai-btn sc-ai-btn--secondary");
            builder.AddAttribute(21, "onclick", (Action)(() => block.Reject()));
            builder.AddContent(22, "Reject");
            builder.CloseElement();

            builder.CloseElement();
        }
        else
        {
            var approved = block.Status == ApprovalStatus.Approved;
            builder.OpenElement(23, "div");
            builder.AddAttribute(
                24,
                "class",
                approved
                    ? "sc-ai-approval__status sc-ai-approval__status--approved"
                    : "sc-ai-approval__status sc-ai-approval__status--rejected");
            builder.AddContent(25, approved ? "Approved" : "Rejected");
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
