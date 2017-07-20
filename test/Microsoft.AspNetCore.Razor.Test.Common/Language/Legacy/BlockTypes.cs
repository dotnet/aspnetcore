// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    // The product code doesn't need this, but having subclasses for the block types makes tests much cleaner :)

    internal class StatementBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Statement;

        public StatementBlock(IParentChunkGenerator chunkGenerator, IReadOnlyList<SyntaxTreeNode> children)
            : base(ThisBlockKind, children, chunkGenerator)
        {
        }

        public StatementBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IReadOnlyList<SyntaxTreeNode>)children)
        {
        }

        public StatementBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    internal class DirectiveBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Directive;

        public DirectiveBlock(IParentChunkGenerator chunkGenerator, IReadOnlyList<SyntaxTreeNode> children)
            : base(ThisBlockKind, children, chunkGenerator)
        {
        }

        public DirectiveBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IReadOnlyList<SyntaxTreeNode>)children)
        {
        }

        public DirectiveBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    internal class ExpressionBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Expression;

        public ExpressionBlock(IParentChunkGenerator chunkGenerator, IReadOnlyList<SyntaxTreeNode> children)
            : base(ThisBlockKind, children, chunkGenerator)
        {
        }

        public ExpressionBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IReadOnlyList<SyntaxTreeNode>)children)
        {
        }

        public ExpressionBlock(params SyntaxTreeNode[] children)
            : this(new ExpressionChunkGenerator(), children)
        {
        }
    }

    internal class MarkupTagBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Tag;

        public MarkupTagBlock(params SyntaxTreeNode[] children)
            : base(ThisBlockKind, children, ParentChunkGenerator.Null)
        {
        }
    }

    internal class MarkupBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Markup;

        public MarkupBlock(
            BlockKindInternal BlockKind,
            IParentChunkGenerator chunkGenerator,
            IReadOnlyList<SyntaxTreeNode> children)
            : base(BlockKind, children, chunkGenerator)
        {
        }

        public MarkupBlock(IParentChunkGenerator chunkGenerator, IReadOnlyList<SyntaxTreeNode> children)
            : this(ThisBlockKind, chunkGenerator, children)
        {
        }

        public MarkupBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IReadOnlyList<SyntaxTreeNode>)children)
        {
        }

        public MarkupBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    internal class MarkupTagHelperBlock : TagHelperBlock
    {
        public MarkupTagHelperBlock(string tagName)
            : this(tagName, tagMode: TagMode.StartTagAndEndTag, attributes: new List<TagHelperAttributeNode>())
        {
        }

        public MarkupTagHelperBlock(string tagName, TagMode tagMode)
            : this(tagName, tagMode, new List<TagHelperAttributeNode>())
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            IList<TagHelperAttributeNode> attributes)
            : this(tagName, TagMode.StartTagAndEndTag, attributes, children: new SyntaxTreeNode[0])
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            TagMode tagMode,
            IList<TagHelperAttributeNode> attributes)
            : this(tagName, tagMode, attributes, new SyntaxTreeNode[0])
        {
        }

        public MarkupTagHelperBlock(string tagName, params SyntaxTreeNode[] children)
            : this(
                  tagName,
                  TagMode.StartTagAndEndTag,
                  attributes: new List<TagHelperAttributeNode>(),
                  children: children)
        {
        }

        public MarkupTagHelperBlock(string tagName, TagMode tagMode, params SyntaxTreeNode[] children)
            : this(tagName, tagMode, new List<TagHelperAttributeNode>(), children)
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            IList<TagHelperAttributeNode> attributes,
            params SyntaxTreeNode[] children)
            : base(new TagHelperBlockBuilder(
                tagName,
                TagMode.StartTagAndEndTag,
                attributes: attributes,
                children: children))
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            TagMode tagMode,
            IList<TagHelperAttributeNode> attributes,
            params SyntaxTreeNode[] children)
            : base(new TagHelperBlockBuilder(tagName, tagMode, attributes, children))
        {
        }
    }

    internal class TemplateBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Template;

        public TemplateBlock(IParentChunkGenerator chunkGenerator, IReadOnlyList<SyntaxTreeNode> children)
            : base(ThisBlockKind, children, chunkGenerator)
        {
        }

        public TemplateBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IReadOnlyList<SyntaxTreeNode>)children)
        {
        }

        public TemplateBlock(params SyntaxTreeNode[] children)
            : this(new TemplateBlockChunkGenerator(), children)
        {
        }

        public TemplateBlock(IReadOnlyList<SyntaxTreeNode> children)
            : this(new TemplateBlockChunkGenerator(), children)
        {
        }
    }

    internal class CommentBlock : Block
    {
        private const BlockKindInternal ThisBlockKind = BlockKindInternal.Comment;

        public CommentBlock(IParentChunkGenerator chunkGenerator, IReadOnlyList<SyntaxTreeNode> children)
            : base(ThisBlockKind, children, chunkGenerator)
        {
        }

        public CommentBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IReadOnlyList<SyntaxTreeNode>)children)
        {
        }

        public CommentBlock(params SyntaxTreeNode[] children)
            : this(new RazorCommentChunkGenerator(), children)
        {
        }

        public CommentBlock(IReadOnlyList<SyntaxTreeNode> children)
            : this(new RazorCommentChunkGenerator(), children)
        {
        }
    }
}
