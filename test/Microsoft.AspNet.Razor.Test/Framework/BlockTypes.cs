// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    // The product code doesn't need this, but having subclasses for the block types makes tests much cleaner :)

    public class StatementBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Statement;

        public StatementBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public StatementBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public StatementBlock(params SyntaxTreeNode[] children)
            : this(BlockCodeGenerator.Null, children)
        {
        }

        public StatementBlock(IEnumerable<SyntaxTreeNode> children)
            : this(BlockCodeGenerator.Null, children)
        {
        }
    }

    public class DirectiveBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Directive;

        public DirectiveBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public DirectiveBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public DirectiveBlock(params SyntaxTreeNode[] children)
            : this(BlockCodeGenerator.Null, children)
        {
        }

        public DirectiveBlock(IEnumerable<SyntaxTreeNode> children)
            : this(BlockCodeGenerator.Null, children)
        {
        }
    }

    public class FunctionsBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Functions;

        public FunctionsBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public FunctionsBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public FunctionsBlock(params SyntaxTreeNode[] children)
            : this(BlockCodeGenerator.Null, children)
        {
        }

        public FunctionsBlock(IEnumerable<SyntaxTreeNode> children)
            : this(BlockCodeGenerator.Null, children)
        {
        }
    }

    public class ExpressionBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Expression;

        public ExpressionBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public ExpressionBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public ExpressionBlock(params SyntaxTreeNode[] children)
            : this(new ExpressionCodeGenerator(), children)
        {
        }

        public ExpressionBlock(IEnumerable<SyntaxTreeNode> children)
            : this(new ExpressionCodeGenerator(), children)
        {
        }
    }

    public class HelperBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Helper;

        public HelperBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public HelperBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public HelperBlock(params SyntaxTreeNode[] children)
            : this(BlockCodeGenerator.Null, children)
        {
        }

        public HelperBlock(IEnumerable<SyntaxTreeNode> children)
            : this(BlockCodeGenerator.Null, children)
        {
        }
    }

    public class MarkupBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Markup;

        public MarkupBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public MarkupBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public MarkupBlock(params SyntaxTreeNode[] children)
            : this(BlockCodeGenerator.Null, children)
        {
        }

        public MarkupBlock(IEnumerable<SyntaxTreeNode> children)
            : this(BlockCodeGenerator.Null, children)
        {
        }
    }

    public class SectionBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Section;

        public SectionBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public SectionBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public SectionBlock(params SyntaxTreeNode[] children)
            : this(BlockCodeGenerator.Null, children)
        {
        }

        public SectionBlock(IEnumerable<SyntaxTreeNode> children)
            : this(BlockCodeGenerator.Null, children)
        {
        }
    }

    public class TemplateBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Template;

        public TemplateBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public TemplateBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public TemplateBlock(params SyntaxTreeNode[] children)
            : this(new TemplateBlockCodeGenerator(), children)
        {
        }

        public TemplateBlock(IEnumerable<SyntaxTreeNode> children)
            : this(new TemplateBlockCodeGenerator(), children)
        {
        }
    }

    public class CommentBlock : Block
    {
        private const BlockType ThisBlockType = BlockType.Comment;

        public CommentBlock(IBlockCodeGenerator codeGenerator, IEnumerable<SyntaxTreeNode> children)
            : base(ThisBlockType, children, codeGenerator)
        {
        }

        public CommentBlock(IBlockCodeGenerator codeGenerator, params SyntaxTreeNode[] children)
            : this(codeGenerator, (IEnumerable<SyntaxTreeNode>)children)
        {
        }

        public CommentBlock(params SyntaxTreeNode[] children)
            : this(new RazorCommentCodeGenerator(), children)
        {
        }

        public CommentBlock(IEnumerable<SyntaxTreeNode> children)
            : this(new RazorCommentCodeGenerator(), children)
        {
        }
    }
}
