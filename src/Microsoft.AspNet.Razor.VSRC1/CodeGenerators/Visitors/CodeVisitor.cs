// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Chunks;

namespace Microsoft.AspNet.Razor.CodeGenerators.Visitors
{
    public class CodeVisitor<TWriter> : ChunkVisitor<TWriter>
        where TWriter : CodeWriter
    {
        public CodeVisitor(TWriter writer, CodeGeneratorContext context)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        protected override void Visit(LiteralChunk chunk)
        {
        }
        protected override void Visit(ExpressionBlockChunk chunk)
        {
        }
        protected override void Visit(ExpressionChunk chunk)
        {
        }
        protected override void Visit(StatementChunk chunk)
        {
        }
        protected override void Visit(UsingChunk chunk)
        {
        }
        protected override void Visit(ParentChunk chunk)
        {
        }
        protected override void Visit(DynamicCodeAttributeChunk chunk)
        {
        }
        protected override void Visit(TagHelperChunk chunk)
        {
        }
        protected override void Visit(TagHelperPrefixDirectiveChunk chunk)
        {
        }
        protected override void Visit(AddTagHelperChunk chunk)
        {
        }
        protected override void Visit(RemoveTagHelperChunk chunk)
        {
        }
        protected override void Visit(LiteralCodeAttributeChunk chunk)
        {
        }
        protected override void Visit(CodeAttributeChunk chunk)
        {
        }
        protected override void Visit(SectionChunk chunk)
        {
        }
        protected override void Visit(TypeMemberChunk chunk)
        {
        }
        protected override void Visit(SetBaseTypeChunk chunk)
        {
        }
        protected override void Visit(TemplateChunk chunk)
        {
        }
    }
}
