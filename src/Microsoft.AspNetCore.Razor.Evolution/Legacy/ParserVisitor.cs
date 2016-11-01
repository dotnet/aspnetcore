// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal abstract class ParserVisitor
    {
        public virtual void VisitBlock(Block block)
        {
            VisitStartBlock(block);
            
            for (var i = 0; i < block.Children.Count; i++)
            {
                block.Children[i].Accept(this);
            }

            VisitEndBlock(block);
        }

        public virtual void VisitStartBlock(Block block)
        {
            if (block.ChunkGenerator != null)
            {
                block.ChunkGenerator.AcceptStart(this, block);
            }
        }

        public virtual void VisitEndBlock(Block block)
        {
            if (block.ChunkGenerator != null)
            {
                block.ChunkGenerator.AcceptEnd(this, block);
            }
        }

        public virtual void VisitSpan(Span span)
        {
            if (span.ChunkGenerator != null)
            {
                span.ChunkGenerator.Accept(this, span);
            }
        }

        public virtual void VisitStartDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitEndDynamicAttributeBlock(DynamicAttributeBlockChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitStartExpressionBlock(ExpressionChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitEndExpressionBlock(ExpressionChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitStartAttributeBlock(AttributeBlockChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitEndAttributeBlock(AttributeBlockChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitExpressionSpan(ExpressionChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitSetBaseTypeSpan(SetBaseTypeChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitTagHelperPrefixSpan(TagHelperPrefixDirectiveChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitTypeMemberSpan(TypeMemberChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitMarkupSpan(MarkupChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitImportSpan(AddImportChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitStatementSpan(StatementChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitLiteralAttributeSpan(LiteralAttributeChunkGenerator chunk, Span span)
        {
        }

        public virtual void VisitEndTemplateBlock(TemplateBlockChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitStartTemplateBlock(TemplateBlockChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitStartSectionBlock(SectionChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitEndSectionBlock(SectionChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitEndCommentBlock(RazorCommentChunkGenerator chunk, Block block)
        {
        }

        public virtual void VisitStartCommentBlock(RazorCommentChunkGenerator chunk, Block block)
        {
        }
    }
}
