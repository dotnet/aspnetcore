// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace DojoAgent;

/// <summary>Produces native structured snapshots for the rich-text rendering scenario.</summary>
public sealed class StructuredRichTextChatClient : IChatClient
{
    /// <summary>The scenario endpoint and keyed client registration.</summary>
    public const string Endpoint = "/rich-text";

    /// <summary>Creates the shared model used by both dojo backends.</summary>
    public static IChatClient Create() => new StructuredRichTextChatClient();

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString("N");
        yield return CreateUpdate(
            messageId,
            "Components.AI rich text",
            [
                Node<HeadingNode>(new TextNode("Components.AI rich text"), heading => heading.Level = 2),
                Node<ParagraphNode>(
                    new TextNode("Streaming "),
                    Node<StrongNode>(new TextNode("structured content"))),
            ]);

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        yield return CreateUpdate(messageId, "Complete structured content", CreateContentMatrix());
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private static ChatResponseUpdate CreateUpdate(
        string messageId,
        string text,
        IReadOnlyList<RichTextNode> nodes)
        => new()
        {
            Role = ChatRole.Assistant,
            MessageId = messageId,
            Contents = [new RichTextContent(text, nodes)],
        };

    private static IReadOnlyList<RichTextNode> CreateContentMatrix()
    {
        var safeLink = Node<LinkNode>(
            new TextNode("Components documentation"),
            link =>
            {
                link.Url = "https://learn.microsoft.com/aspnet/core/blazor/";
                link.Title = "Blazor documentation";
            });
        var image = new ImageNode("/rich-text-image.svg", "Decorative rich text sample");
        var task = Node<ListItemNode>(
            Node<ParagraphNode>(new TextNode("Structured snapshots")));
        task.Checked = true;
        var table = new TableNode
        {
            Alignment = [TableColumnAlignment.Left, TableColumnAlignment.Right],
        };
        table.AddChild(Node<TableRowNode>(
            Node<TableCellNode>(new TextNode("Node")),
            Node<TableCellNode>(new TextNode("Rendered as"))));
        table.AddChild(Node<TableRowNode>(
            Node<TableCellNode>(new TextNode("StrongNode")),
            Node<TableCellNode>(new TextNode("strong"))));

        var footnoteDefinition = Node<FootnoteDefinitionNode>(
            Node<ParagraphNode>(new TextNode("Snapshots replace the complete tree.")));
        footnoteDefinition.Label = "snapshot";

        return
        [
            Node<HeadingNode>(
                new TextNode("Components.AI rich text"),
                heading => heading.Level = 2),
            Node<ParagraphNode>(
                new TextNode("Render "),
                Node<StrongNode>(new TextNode("strong")),
                new TextNode(", "),
                Node<EmphasisNode>(new TextNode("emphasized")),
                new TextNode(", and "),
                Node<StrikethroughNode>(new TextNode("struck-through")),
                new TextNode(" text with "),
                new InlineCodeNode("C#"),
                new TextNode(" and "),
                safeLink,
                new TextNode(".")),
            Node<BlockQuoteNode>(
                Node<ParagraphNode>(new TextNode("Streaming never exposes a partial tree."))),
            Node<ListNode>(
                Node<ListItemNode>(Node<ParagraphNode>(new TextNode("Headings and paragraphs"))),
                task),
            new CodeBlockNode("Console.WriteLine(\"Rich text\");", "csharp"),
            new ThematicBreakNode(),
            table,
            image,
            Node<ParagraphNode>(
                new TextNode("Snapshot semantics"),
                new FootnoteReferenceNode { Label = "snapshot" }),
            footnoteDefinition,
            new DefinitionNode
            {
                Label = "components",
                Url = "https://learn.microsoft.com/aspnet/core/blazor/",
                Title = "Components",
            },
            new ImageReferenceNode
            {
                Label = "sample",
                Alt = "Image reference fallback",
                ReferenceKind = ReferenceKind.Collapsed,
            },
            new HtmlNode("<mark>Encoded HTML source</mark>"),
        ];
    }

    private static TNode Node<TNode>(params RichTextNode[] children)
        where TNode : RichTextNode, new()
        => Node<TNode>(children, configure: null);

    private static TNode Node<TNode>(
        RichTextNode child,
        Action<TNode> configure)
        where TNode : RichTextNode, new()
        => Node<TNode>([child], configure);

    private static TNode Node<TNode>(
        RichTextNode[] children,
        Action<TNode>? configure)
        where TNode : RichTextNode, new()
    {
        var node = new TNode();
        configure?.Invoke(node);
        foreach (var child in children)
        {
            node.AddChild(child);
        }

        return node;
    }
}
