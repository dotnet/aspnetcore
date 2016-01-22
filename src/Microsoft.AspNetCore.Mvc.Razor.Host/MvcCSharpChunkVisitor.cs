// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public abstract class MvcCSharpChunkVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public MvcCSharpChunkVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
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

        public override void Accept(Chunk chunk)
        {
            if (chunk is InjectChunk)
            {
                Visit((InjectChunk)chunk);
            }
            else
            {
                base.Accept(chunk);
            }
        }

        protected abstract void Visit(InjectChunk chunk);
    }
}