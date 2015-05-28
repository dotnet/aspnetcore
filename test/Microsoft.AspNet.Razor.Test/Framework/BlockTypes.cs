// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks.Generators;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    // The product code doesn't need this, but having subclasses for the block types makes tests much cleaner :)

    public class StatementBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Statement;

        public StatementBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public StatementBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public StatementBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }

        public StatementBlock(IEnumerable<SyntaxTreeNode> children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    public class DirectiveBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Directive;

        public DirectiveBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public DirectiveBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public DirectiveBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }

        public DirectiveBlock(IEnumerable<SyntaxTreeNode> children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    public class FunctionsBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Functions;

        public FunctionsBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public FunctionsBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public FunctionsBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }

        public FunctionsBlock(IEnumerable<SyntaxTreeNode> children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    public class ExpressionBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Expression;

        public ExpressionBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public ExpressionBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public ExpressionBlock(params SyntaxTreeNode[] children)
            : this(new ExpressionChunkGenerator(), children)
        {
        }

        public ExpressionBlock(IEnumerable<SyntaxTreeNode> children)
            : this(new ExpressionChunkGenerator(), children)
        {
        }
    }

    public class MarkupTagBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Tag;

        public MarkupTagBlock(params SyntaxTreeNode[] children)
            : base(ThisBlockType, children, ParentChunkGenerator.Null)
        {
        }
    }

    public class MarkupBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Markup;

        public MarkupBlock(
            BlockType blockType,
            IParentChunkGenerator chunkGenerator,
            IEnumerable<SyntaxTreeNode> children)
            : base(blockType, children, chunkGenerator)
        {
        }

        public MarkupBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : this(ThisBlockType, chunkGenerator, children)
        {
        }

        public MarkupBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public MarkupBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }

        public MarkupBlock(IEnumerable<SyntaxTreeNode> children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    public class MarkupTagHelperBlock : TagHelperBlock
    {
        public MarkupTagHelperBlock(string tagName)
            : this(tagName, selfClosing: false, attributes: new List<KeyValuePair<string, SyntaxTreeNode>>())
        {
        }

        public MarkupTagHelperBlock(string tagName, bool selfClosing)
            : this(tagName, selfClosing, new List<KeyValuePair<string, SyntaxTreeNode>>())
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            IList<KeyValuePair<string, SyntaxTreeNode>> attributes)
            : this(tagName, selfClosing: false, attributes: attributes, children: new SyntaxTreeNode[0])
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            bool selfClosing,
            IList<KeyValuePair<string, SyntaxTreeNode>> attributes)
            : this(tagName, selfClosing, attributes, new SyntaxTreeNode[0])
        {
        }

        public MarkupTagHelperBlock(string tagName, params SyntaxTreeNode[] children)
            : this(
                  tagName,
                  selfClosing: false,
                  attributes: new List<KeyValuePair<string, SyntaxTreeNode>>(),
                  children: children)
        {
        }

        public MarkupTagHelperBlock(string tagName, bool selfClosing, params SyntaxTreeNode[] children)
            : this(tagName, selfClosing, new List<KeyValuePair<string, SyntaxTreeNode>>(), children)
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            IList<KeyValuePair<string, SyntaxTreeNode>> attributes,
            params SyntaxTreeNode[] children)
            : base(new TagHelperBlockBuilder(tagName, selfClosing: false, attributes: attributes, children: children))
        {
        }

        public MarkupTagHelperBlock(
            string tagName,
            bool selfClosing,
            IList<KeyValuePair<string, SyntaxTreeNode>> attributes,
            params SyntaxTreeNode[] children)
            : base(new TagHelperBlockBuilder(tagName, selfClosing, attributes, children))
        {
        }
    }

    public class SectionBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Section;

        public SectionBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public SectionBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public SectionBlock(params SyntaxTreeNode[] children)
            : this(ParentChunkGenerator.Null, children)
        {
        }

        public SectionBlock(IEnumerable<SyntaxTreeNode> children)
            : this(ParentChunkGenerator.Null, children)
        {
        }
    }

    public class TemplateBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Template;

        public TemplateBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public TemplateBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public TemplateBlock(params SyntaxTreeNode[] children)
            : this(new TemplateBlockChunkGenerator(), children)
        {
        }

        public TemplateBlock(IEnumerable<SyntaxTreeNode> children)
            : this(new TemplateBlockChunkGenerator(), children)
        {
        }
    }

    public class CommentBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Comment;

        public CommentBlock(IParentChunkGenerator chunkGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, chunkGenerator)
        {
        }

        public CommentBlock(IParentChunkGenerator chunkGenerator, params SyntaxTreeNode[] children)
            : this(chunkGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public CommentBlock(params SyntaxTreeNode[] children)
            : this(new RazorCommentChunkGenerator(), children)
        {
        }

        public CommentBlock(IEnumerable<SyntaxTreeNode> children)
            : this(new RazorCommentChunkGenerator(), children)
        {
        }
    }
}
