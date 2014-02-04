using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public abstract class ChunkVisitor : IChunkVisitor
    {
        public void Accept(IList<Chunk> chunks)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException("chunks");
            }

            foreach (Chunk chunk in chunks)
            {
                Accept(chunk);
            }
        }

        public void Accept(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException("chunk");
            }

            if (chunk is LiteralChunk)
            {
                Visit((LiteralChunk)chunk);
            }
            else if (chunk is ExpressionBlockChunk)
            {
                Visit((ExpressionBlockChunk)chunk);
            }
            else if (chunk is ExpressionChunk)
            {
                Visit((ExpressionChunk)chunk);
            }
            else if (chunk is StatementChunk)
            {
                Visit((StatementChunk)chunk);
            }
            else if(chunk is SetLayoutChunk)
            {
                Visit((SetLayoutChunk)chunk);
            }
            else if (chunk is ResolveUrlChunk)
            {
                Visit((ResolveUrlChunk)chunk);
            }
            else if (chunk is TypeMemberChunk)
            {
                Visit((TypeMemberChunk)chunk);
            }            
            else if (chunk is UsingChunk)
            {
                Visit((UsingChunk)chunk);
            }
            else if(chunk is HelperChunk)
            {
                Visit((HelperChunk)chunk);
            }
            else if (chunk is SetBaseTypeChunk)
            {
                Visit((SetBaseTypeChunk)chunk);
            }
            else if (chunk is DynamicCodeAttributeChunk)
            {
                Visit((DynamicCodeAttributeChunk)chunk);
            }
            else if (chunk is LiteralCodeAttributeChunk)
            {
                Visit((LiteralCodeAttributeChunk)chunk);
            }
            else if (chunk is CodeAttributeChunk)
            {
                Visit((CodeAttributeChunk)chunk);
            }
            else if (chunk is SectionChunk)
            {
                Visit((SectionChunk)chunk);
            }
            else if (chunk is TemplateChunk)
            {
                Visit((TemplateChunk)chunk);
            }
            else if (chunk is ChunkBlock)
            {
                Visit((ChunkBlock)chunk);
            }
            else if(chunk is SessionStateChunk)
            {
                Visit((SessionStateChunk)chunk);
            }
            else
            {
                throw new InvalidOperationException("Unknown chunk type " + chunk.GetType().Name);
            }
        }

        protected abstract void Visit(LiteralChunk chunk);
        protected abstract void Visit(ExpressionChunk chunk);
        protected abstract void Visit(StatementChunk chunk);
        protected abstract void Visit(UsingChunk chunk);
        protected abstract void Visit(ChunkBlock chunk);
        protected abstract void Visit(DynamicCodeAttributeChunk chunk);
        protected abstract void Visit(LiteralCodeAttributeChunk chunk);
        protected abstract void Visit(CodeAttributeChunk chunk);
        protected abstract void Visit(HelperChunk chunk);
        protected abstract void Visit(SectionChunk chunk);
        protected abstract void Visit(TypeMemberChunk chunk);
        protected abstract void Visit(ResolveUrlChunk chunk);
        protected abstract void Visit(SetBaseTypeChunk chunk);
        protected abstract void Visit(TemplateChunk chunk);
        protected abstract void Visit(SetLayoutChunk chunk);
        protected abstract void Visit(ExpressionBlockChunk chunk);
        protected abstract void Visit(SessionStateChunk chunk);
    }
}
