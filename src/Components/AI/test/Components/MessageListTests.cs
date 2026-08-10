// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class MessageListTests
{
    [Fact]
    public async Task RendersTurns_AfterMessageSent()
    {
        var cut = RenderMessageList(_ => ResponseEmitters.EmitTextResponse("Hello!"));
        var context = GetAgentContext(cut);

        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-turn", html);
        Assert.Contains("Hi", html);
        Assert.Contains("Hello!", html);
    }

    [Fact]
    public async Task MultipleTurns_AllRendered()
    {
        var callCount = 0;
        var cut = RenderMessageList(_ => ResponseEmitters.EmitTextResponse($"Response {++callCount}"));
        var context = GetAgentContext(cut);

        await cut.InvokeAsync(() => context.SendMessageAsync("First"));
        await cut.InvokeAsync(() => context.SendMessageAsync("Second"));

        var html = cut.GetHtml();
        Assert.Contains("Response 1", html);
        Assert.Contains("Response 2", html);
        Assert.Equal(2, CountOccurrences(html, "sc-ai-turn sc-ai-turn--user"));
    }

    [Fact]
    public async Task StreamingBlock_RendersStreamingModifierUntilComplete()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderMessageList(ct => ResponseEmitters.EmitTokensWithGate(
            ["Partial", " complete"],
            async index =>
            {
                if (index == 1)
                {
                    await gate.Task;
                }
            },
            ct));
        var context = GetAgentContext(cut);

        var sendTask = cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        await WaitForHtmlAsync(cut, "Partial");
        Assert.Contains("sc-ai-message__content--streaming", cut.GetHtml());

        gate.SetResult();
        await sendTask;

        var html = cut.GetHtml();
        Assert.Contains("Partial complete", html);
        Assert.DoesNotContain("sc-ai-message__content--streaming", html);
    }

    [Fact]
    public async Task RichTextContent_RendersStructuredNodesAndEncodesUnsafeContent()
    {
        var cut = RenderMessageList(_ => EmitRichTextResponse());
        var context = GetAgentContext(cut);

        await cut.InvokeAsync(() => context.SendMessageAsync("Format this"));

        var html = cut.GetHtml();
        Assert.Contains("<h2", html);
        Assert.Contains("<strong>structured</strong>", html);
        Assert.Contains("<em>rich text</em>", html);
        Assert.Contains("<s>obsolete</s>", html);
        Assert.Contains("class=\"sc-ai-rich-text__inline-code\"", html);
        Assert.Contains("<blockquote", html);
        Assert.Contains("<ul", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("<pre", html);
        Assert.Contains("<table", html);
        Assert.Contains("href=\"https://example.com/docs\"", html);
        Assert.DoesNotContain("href=\"javascript:", html);
        Assert.Contains("&lt;script&gt;alert(", html);
        Assert.Contains("&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void EmptyConversation_RendersEmptyContent()
    {
        var cut = RenderMessageList(
            _ => ResponseEmitters.EmitTextResponse("unused"),
            emptyContent: builder => builder.AddMarkupContent(0, "<p>Nothing here yet</p>"));

        Assert.Contains("Nothing here yet", cut.GetHtml());
    }

    [Fact]
    public async Task Error_RendersRetryAffordance()
    {
        var cut = RenderMessageList(_ =>
            ResponseEmitters.EmitErrorAfterTokens([], new InvalidOperationException("boom")));
        var context = GetAgentContext(cut);

        await cut.InvokeAsync(() => context.SendMessageAsync("Hi"));

        var html = cut.GetHtml();
        Assert.Contains("sc-ai-error", html);
        Assert.Contains("type=\"button\"", html);
        Assert.Contains("Retry", html);
    }

    [Fact]
    public void MostRecentlyRegisteredBlockRenderer_Wins()
    {
        var context = new MessageListContext();
        RenderFragment first = _ => { };
        RenderFragment second = _ => { };
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(RichContentBlock),
            When = null,
            Render = _ => first,
        });
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(RichContentBlock),
            When = null,
            Render = _ => second,
        });

        var result = context.RenderBlock(new RichContentBlock());

        Assert.Same(second, result);
    }

    [Fact]
    public void BlockRenderer_WithoutChildContent_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RenderMessageList(
                _ => ResponseEmitters.EmitTextResponse("unused"),
                childContent: builder =>
                {
                    builder.OpenComponent<BlockRenderer<RichContentBlock>>(0);
                    builder.CloseComponent();
                }));

        Assert.Equal("BlockRenderer requires child content.", exception.Message);
    }

    private static RenderedComponent<AgentBoundary> RenderMessageList(
        Func<CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> respond,
        RenderFragment? emptyContent = null,
        RenderFragment? childContent = null)
    {
        var client = new DelegatingStreamingChatClient();
        client.SetHandler((msgs, opts, ct) => respond(ct));
        var agent = new UIAgent(client);

        var renderer = new TestRenderer();
        return renderer.RenderComponent<AgentBoundary>(p =>
        {
            p["Agent"] = agent;
            p["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MessageList>(0);
                if (emptyContent is not null)
                {
                    builder.AddComponentParameter(1, "EmptyContent", emptyContent);
                }
                if (childContent is not null)
                {
                    builder.AddComponentParameter(2, "ChildContent", childContent);
                }
                builder.CloseComponent();
            });
        });
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> EmitRichTextResponse()
    {
        var heading = Node<HeadingNode>(new TextNode("Components.AI"));
        heading.Level = 2;
        var paragraph = Node<ParagraphNode>(
            new TextNode("Render "),
            Node<StrongNode>(new TextNode("structured")),
            new TextNode(" "),
            Node<EmphasisNode>(new TextNode("rich text")),
            new TextNode(" and "),
            Node<StrikethroughNode>(new TextNode("obsolete")),
            new TextNode(" markup with "),
            new InlineCodeNode("C#"),
            new TextNode("."));
        var safeLink = new LinkNode("https://example.com/docs");
        safeLink.AddChild(new TextNode("Documentation"));
        var unsafeLink = new LinkNode("javascript:alert('unsafe')");
        unsafeLink.AddChild(new TextNode("Unsafe"));
        var list = new ListNode();
        list.AddChild(Node<ListItemNode>(Node<ParagraphNode>(new TextNode("First item"))));
        var checkedItem = Node<ListItemNode>(Node<ParagraphNode>(new TextNode("Completed item")));
        checkedItem.Checked = true;
        list.AddChild(checkedItem);
        var table = new TableNode
        {
            Alignment = [TableColumnAlignment.Left, TableColumnAlignment.Right],
        };
        table.AddChild(Node<TableRowNode>(
            Node<TableCellNode>(new TextNode("Feature")),
            Node<TableCellNode>(new TextNode("Status"))));

        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = "rich-text",
            Contents =
            [
                new RichTextContent(
                    "Components.AI structured rich text",
                    [
                        heading,
                        paragraph,
                        Node<BlockQuoteNode>(
                            Node<ParagraphNode>(new TextNode("Streaming snapshot"))),
                        list,
                        new CodeBlockNode("Console.WriteLine(\"Hello\");", "csharp"),
                        safeLink,
                        unsafeLink,
                        table,
                        new HtmlNode("<script>alert('unsafe')</script>"),
                    ]),
            ],
        };

        await Task.CompletedTask;
    }

    private static TNode Node<TNode>(params RichTextNode[] children)
        where TNode : RichTextNode, new()
    {
        var node = new TNode();
        foreach (var child in children)
        {
            node.AddChild(child);
        }

        return node;
    }

    private static async Task WaitForHtmlAsync(
        RenderedComponent<AgentBoundary> cut, string expected)
    {
        for (var i = 0; i < 100; i++)
        {
            if (cut.GetHtml().Contains(expected, StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Timed out waiting for '{expected}' to render. Current markup: {cut.GetHtml()}");
    }

    private static AgentContext GetAgentContext(RenderedComponent<AgentBoundary> cut)
    {
        return (AgentContext)typeof(AgentBoundary)
            .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
