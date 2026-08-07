// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class MessageListContextTests
{
    [Fact]
    public void RenderBlock_RichContentBlock_RendersRawText()
    {
        var context = new MessageListContext();
        var block = new RichContentBlock { Role = ChatRole.Assistant };
        block.AppendText("Hello ");
        block.AppendText("world");

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-message", html);
        Assert.Contains("Hello world", html);
    }

    [Fact]
    public void RenderBlock_NonRichBlock_RendersTypeName()
    {
        var context = new MessageListContext();
        var block = new TestContentBlock();

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-tool-call", html);
        Assert.Contains("TestContentBlock", html);
    }

    [Fact]
    public void RenderBlock_CustomRegistration_MatchesByType()
    {
        var context = new MessageListContext();
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(RichContentBlock),
            Render = block => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom");
                builder.AddContent(2, ((RichContentBlock)block).RawText);
                builder.CloseElement();
            }
        });

        var block = new RichContentBlock { Role = ChatRole.Assistant };
        block.AppendText("Custom render");

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("class=\"custom\"", html);
        Assert.Contains("Custom render", html);
        Assert.DoesNotContain("class=\"block\"", html);
    }

    [Fact]
    public void RenderBlock_CustomRegistration_WithWhenPredicate_OnlyMatchesWhenTrue()
    {
        var context = new MessageListContext();
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(RichContentBlock),
            When = block => ((RichContentBlock)block).RawText.Contains("special"),
            Render = block => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "special");
                builder.AddContent(2, ((RichContentBlock)block).RawText);
                builder.CloseElement();
            }
        });

        // Block that doesn't match the predicate - should fall through to default
        var normalBlock = new RichContentBlock { Role = ChatRole.Assistant };
        normalBlock.AppendText("Normal text");

        var renderer = new TestRenderer();
        var normalRendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(normalBlock);
        });
        Assert.Contains("sc-ai-message", normalRendered.GetHtml());
        Assert.DoesNotContain("class=\"special\"", normalRendered.GetHtml());

        // Block that matches the predicate - should use custom renderer
        var specialBlock = new RichContentBlock { Role = ChatRole.Assistant };
        specialBlock.AppendText("special content");

        var specialRendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(specialBlock);
        });
        Assert.Contains("class=\"special\"", specialRendered.GetHtml());
    }

    [Fact]
    public void RenderBlock_MultipleRegistrations_FirstMatchWins()
    {
        var context = new MessageListContext();
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(RichContentBlock),
            Render = block => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "first");
                builder.CloseElement();
            }
        });
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(RichContentBlock),
            Render = block => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "second");
                builder.CloseElement();
            }
        });

        var block = new RichContentBlock { Role = ChatRole.Assistant };
        block.AppendText("test");

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("class=\"first\"", html);
        Assert.DoesNotContain("class=\"second\"", html);
    }

    [Fact]
    public void RenderBlock_BaseTypeRegistration_MatchesDerivedBlocks()
    {
        var context = new MessageListContext();
        context.AddRegistration(new BlockRendererRegistration
        {
            BlockType = typeof(ContentBlock),
            Render = block => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "catch-all");
                builder.AddContent(2, block.GetType().Name);
                builder.CloseElement();
            }
        });

        var block = new RichContentBlock { Role = ChatRole.Assistant };
        block.AppendText("test");

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("class=\"catch-all\"", html);
        Assert.Contains("RichContentBlock", html);
    }

    [Fact]
    public void RenderBlock_MediaContentBlock_RendersImage()
    {
        var context = new MessageListContext();
        var block = new MediaContentBlock { Role = ChatRole.User };
        block.AddContent(new DataContent(new byte[] { 1, 2, 3 }, "image/png"));

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-media", html);
        Assert.Contains("sc-ai-media__image", html);
        Assert.Contains("<img", html);
        Assert.Contains("data:image/png;base64,", html);
    }

    [Fact]
    public void RenderBlock_MediaContentBlock_RendersAudio()
    {
        var context = new MessageListContext();
        var block = new MediaContentBlock { Role = ChatRole.Assistant };
        block.AddContent(new DataContent(new byte[] { 0xFF, 0xFB }, "audio/mpeg"));

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-media__audio", html);
        Assert.Contains("<audio", html);
        Assert.Contains("controls", html);
    }

    [Fact]
    public void RenderBlock_MediaContentBlock_RendersVideo()
    {
        var context = new MessageListContext();
        var block = new MediaContentBlock { Role = ChatRole.User };
        block.AddContent(new DataContent(new byte[] { 0, 0 }, "video/mp4"));

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-media__video", html);
        Assert.Contains("<video", html);
    }

    [Fact]
    public void RenderBlock_MediaContentBlock_UnknownType_RendersFileIndicator()
    {
        var context = new MessageListContext();
        var block = new MediaContentBlock { Role = ChatRole.User };
        block.AddContent(new DataContent(new byte[] { 0 }, "application/pdf"));

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-media__file", html);
        Assert.Contains("application/pdf", html);
    }

    [Fact]
    public void RenderBlock_MediaContentBlock_UserRole_HasUserClass()
    {
        var context = new MessageListContext();
        var block = new MediaContentBlock { Role = ChatRole.User };
        block.AddContent(new DataContent(new byte[] { 1 }, "image/png"));

        var renderer = new TestRenderer();
        var rendered = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = context.RenderBlock(block);
        });

        var html = rendered.GetHtml();
        Assert.Contains("sc-ai-message--user", html);
    }

    // Helper component to render a RenderFragment
    private class FragmentHost : ComponentBase
    {
        [Parameter]
        public RenderFragment? Fragment { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Fragment is not null)
            {
                builder.AddContent(0, Fragment);
            }
        }
    }

    private class TestContentBlock : ContentBlock
    {
    }
}
