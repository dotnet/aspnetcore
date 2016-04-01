// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public abstract class ChunkVisitor<TWriter> : IChunkVisitor
        where TWriter : CodeWriter
    {
        public ChunkVisitor(TWriter writer, CodeGeneratorContext context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Writer = writer;
            Context = context;
        }

        protected TWriter Writer { get; private set; }
        protected CodeGeneratorContext Context { get; private set; }

        public void Accept(IList<Chunk> chunks)
        {
            if (chunks == null)
            {
                throw new ArgumentNullException(nameof(chunks));
            }

            foreach (Chunk chunk in chunks)
            {
                Accept(chunk);
            }
        }

        public virtual void Accept(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
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
            else if (chunk is TagHelperChunk)
            {
                Visit((TagHelperChunk)chunk);
            }
            else if (chunk is TagHelperPrefixDirectiveChunk)
            {
                Visit((TagHelperPrefixDirectiveChunk)chunk);
            }
            else if (chunk is AddTagHelperChunk)
            {
                Visit((AddTagHelperChunk)chunk);
            }
            else if (chunk is RemoveTagHelperChunk)
            {
                Visit((RemoveTagHelperChunk)chunk);
            }
            else if (chunk is TypeMemberChunk)
            {
                Visit((TypeMemberChunk)chunk);
            }
            else if (chunk is UsingChunk)
            {
                Visit((UsingChunk)chunk);
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
            else if (chunk is ParentChunk)
            {
                Visit((ParentChunk)chunk);
            }
        }

        protected abstract void Visit(LiteralChunk chunk);
        protected abstract void Visit(ExpressionChunk chunk);
        protected abstract void Visit(StatementChunk chunk);
        protected abstract void Visit(TagHelperChunk chunk);
        protected abstract void Visit(TagHelperPrefixDirectiveChunk chunk);
        protected abstract void Visit(AddTagHelperChunk chunk);
        protected abstract void Visit(RemoveTagHelperChunk chunk);
        protected abstract void Visit(UsingChunk chunk);
        protected abstract void Visit(ParentChunk chunk);
        protected abstract void Visit(DynamicCodeAttributeChunk chunk);
        protected abstract void Visit(LiteralCodeAttributeChunk chunk);
        protected abstract void Visit(CodeAttributeChunk chunk);
        protected abstract void Visit(SectionChunk chunk);
        protected abstract void Visit(TypeMemberChunk chunk);
        protected abstract void Visit(SetBaseTypeChunk chunk);
        protected abstract void Visit(TemplateChunk chunk);
        protected abstract void Visit(ExpressionBlockChunk chunk);
    }
}
