// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal abstract class ParserVisitor
    {
        protected virtual void VisitDefault(Block block)
        {
            for (var i = 0; i < block.Children.Count; i++)
            {
                block.Children[i].Accept(this);
            }
        }

        public virtual void VisitBlock(Block block)
        {
            if (block.ChunkGenerator != null)
            {
                block.ChunkGenerator.Accept(this, block);
            }
        }

        public virtual void VisitSpan(Span span)
        {
            if (span.ChunkGenerator != null)
            {
                span.ChunkGenerator.Accept(this, span);
            }
        }

        public virtual void VisitDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitExpressionBlock(ExpressionChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitAttributeBlock(AttributeBlockChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitExpressionSpan(ExpressionChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitMarkupSpan(MarkupChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitImportSpan(AddImportChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitStatementSpan(StatementChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitDirectiveToken(DirectiveTokenChunkGenerator chunkGenerator, Span block)
        {
        }

        public virtual void VisitDirectiveBlock(DirectiveChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitTemplateBlock(TemplateBlockChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitCommentBlock(RazorCommentChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitTagHelperBlock(TagHelperChunkGenerator chunkGenerator, Block block)
        {
            VisitDefault(block);
        }

        public virtual void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
        {
        }

        public virtual void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
        {
        }
    }
}
